using ArduinoCodeAssistant.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Animation;

namespace ArduinoCodeAssistant.Services
{
    public class ArduinoService
    {
        private readonly ArduinoInfo _arduinoInfo;
        private readonly SerialConfig _serialConfig;
        private readonly LoggingService _loggingService;
        public ArduinoService(ArduinoInfo arduinoInfo, SerialConfig serialConfig, LoggingService loggingService)
        {
            _arduinoInfo = arduinoInfo;
            _serialConfig = serialConfig;
            _loggingService = loggingService;

            _serialConfig.SerialPort.DataReceived += OnDataReceived;
        }

        public async Task<bool> DetectDeviceAsync()
        {
            if (!File.Exists(@"C:\Program Files\Arduino CLI\arduino-cli.exe"))
            {
                throw new Exception("Arduino CLI 설치 파일을 찾을 수 없습니다.");
            }

            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

            bool deviceDetected = false;

            var deviceDetectionTask = Task.Run(() =>
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string? queriedPort = item["DeviceID"]?.ToString();
                    string? queriedName = item["Description"]?.ToString();

                    if (queriedPort != null && queriedName != null && queriedName.Contains("Arduino"))
                    {
                        _arduinoInfo.Port = queriedPort;
                        _arduinoInfo.Name = queriedName;

                        return true;
                    }
                }
                return false;
            });

            deviceDetected = await deviceDetectionTask;

            if (!deviceDetected)
            {
                throw new Exception("아두이노 보드를 찾을 수 없습니다. 기기 연결 상태를 확인하세요.");
            }

            if (_arduinoInfo.Port != null && !await ExtractFqbnAndCoreAsync(_arduinoInfo.Port))
            {
                throw new Exception("fqbn 및 core 추출에 실패했습니다.");
            }

            string coreListOutput = await RunArduinoCliAsync("core list");
            if (_arduinoInfo.Core != null && !coreListOutput.Contains(_arduinoInfo.Core))
            {
                _loggingService.Log($"{_arduinoInfo.Name} 전용 필수 core 설치 중...");
                await RunArduinoCliAsync($"core install {_arduinoInfo.Core}");
                _loggingService.Log("설치 완료.");
            }

            return true;
        }

        public event EventHandler<string> DataReceived;

        public void OpenSerialPort(string? portName)
        {
            if (portName == null || _serialConfig.SerialPort.IsOpen) return;

            _serialConfig.SerialPort.PortName = portName;
            _serialConfig.SerialPort.Open();

            _loggingService.Log($"시리얼 포트 개방 완료. (PortName: {_serialConfig.SerialPort.PortName}, BaudRate: {_serialConfig.SerialPort.BaudRate})");
        }

        public void CloseSerialPort()
        {
            if (!_serialConfig.SerialPort.IsOpen) return;

            _serialConfig.SerialPort.Close();

            _loggingService.Log("시리얼 포트 폐쇄 완료.");
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialConfig.SerialPort == null) return;

            try
            {
                string line = _serialConfig.SerialPort.ReadLine();
                if (line.EndsWith("\r"))
                {
                    line = line.Substring(0, line.Length - 1);
                }
                _loggingService.SerialLog(line);
            }
            catch
            {
                
            }
        }

        public async Task<bool> UploadCodeAsync(string sketchCode)
        {
            if (_arduinoInfo.Port == null || _arduinoInfo.Fqbn == null)
            {
                throw new Exception("아두이노 정보를 불러올 수 없습니다.");
            }

            string port = _arduinoInfo.Port;
            string fqbn = _arduinoInfo.Fqbn;

            string sketchFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempSketch");
            string sketchPath = Path.Combine(sketchFolder, "TempSketch.ino");

            _loggingService.Log("스케치 파일 및 폴더 생성 시작...");

            if (!Directory.Exists(sketchFolder))
            {
                Directory.CreateDirectory(sketchFolder);
            }
            await File.WriteAllTextAsync(sketchPath, sketchCode);

            _loggingService.Log("스케치 파일 및 폴더 생성 완료.");
            _loggingService.Log("코드 컴파일 시작...");
            await RunArduinoCliAsync($"compile -b {fqbn} {sketchFolder}");
            _loggingService.Log("코드 컴파일 완료.");
            _loggingService.Log("코드 업로드 시작...");
            await RunArduinoCliAsync($"upload -p {port} -b {fqbn} {sketchFolder}");
            _loggingService.Log("코드 업로드 완료.");
            return true;
        }

        static async Task<string> RunArduinoCliAsync(string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(@"C:\Program Files\Arduino CLI\arduino-cli.exe", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = processStartInfo };

            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            //Debug.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }

            return output;
        }

        async Task<bool> ExtractFqbnAndCoreAsync(string port)
        {
            int fqbnIndex = -1;
            int coreIndex = -1;

            string boardInfo = await RunArduinoCliAsync("board list");
            string[] lines = boardInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);


            if (lines.Length == 0) return false;

            string header = lines[0];
            fqbnIndex = header.IndexOf("FQBN");
            coreIndex = header.IndexOf("Core");

            if (fqbnIndex == -1 || coreIndex == -1) return false;


            foreach (string line in lines)
            {
                if (line.Length < fqbnIndex || line.Length < coreIndex) break;

                if (line.Contains(port))
                {

                    string fqbnPart = line.Substring(fqbnIndex).Trim();
                    string[] parts1 = fqbnPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts1.Length > 0)
                    {
                        _arduinoInfo.Fqbn = parts1[0];
                    }

                    string corePart = line.Substring(coreIndex).Trim();
                    string[] parts2 = corePart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts2.Length > 0)
                    {
                        _arduinoInfo.Core = parts2[0];
                    }

                    if (!string.IsNullOrEmpty(_arduinoInfo.Fqbn) && !string.IsNullOrEmpty(_arduinoInfo.Core))
                        return true;
                }
            }
            return false;
        }
    }
}
