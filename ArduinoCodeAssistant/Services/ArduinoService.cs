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

namespace ArduinoCodeAssistant.Services
{
    public class ArduinoService
    {
        private readonly ArduinoInfo _arduinoInfo;
        private readonly LoggingService _loggingService;
        public ArduinoService(ArduinoInfo arduinoInfo, LoggingService loggingService)
        {
            _arduinoInfo = arduinoInfo;
            _loggingService = loggingService;
        }

        public async Task<bool> DetectDeviceAsync()
        {
            return await Task.Run(() =>
            {
                _arduinoInfo.SetAllPropertiesToNull();

                if (!File.Exists(@"C:\Program Files\Arduino CLI\arduino-cli.exe"))
                {
                    throw new Exception("Arduino CLI 설치 파일을 찾을 수 없습니다.");
                }

                ManagementScope connectionScope = new ManagementScope();
                SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);
                foreach (ManagementObject item in searcher.Get())
                {
                    string? queriedPort = item["DeviceID"].ToString();
                    string? queriedName = item["Description"].ToString();

                    if (queriedPort != null && queriedName != null && queriedName.Contains("Arduino"))
                    {
                        _loggingService.Log("기기 탐색 완료.");
                        _arduinoInfo.Port = queriedPort;
                        _arduinoInfo.Name = queriedName;


                        if (!ExtractFqbnAndCore(_arduinoInfo.Port, out string fqbn, out string core))
                        {
                            throw new Exception("fqbn 및 core 추출에 실패했습니다.");
                        }
                        _arduinoInfo.Fqbn = fqbn;
                        _arduinoInfo.Core = core;

                        if (!RunArduinoCli("core list").Contains(core))
                        {
                            _loggingService.Log($"{_arduinoInfo.Name} 전용 필수 core 설치 중...");
                            RunArduinoCli($"core install {core}");
                            _loggingService.Log($"설치 완료.");
                        }
                        OpenSerialPort(_arduinoInfo.Port, 9600);
                        return true;
                    }
                }
                throw new Exception("아두이노 보드를 찾을 수 없습니다. 기기 연결 상태를 확인하세요.");
            });
        }

        private SerialPort _serialPort;
        public event EventHandler<string> DataReceived;
        private void OpenSerialPort(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.DataReceived += OnDataReceived;

        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            while (_serialPort.BytesToRead > 0)
            {
                sb.Append(_serialPort.ReadExisting());
            }
            _loggingService.Log(sb.ToString());
        }

        public async Task<bool> UploadCodeAsync(string sketchCode)
        {
            return await Task.Run(() =>
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
                try
                {
                    // 스케치 폴더 생성 (폴더가 존재하지 않는 경우에만 생성)
                    if (!Directory.Exists(sketchFolder))
                    {
                        Directory.CreateDirectory(sketchFolder);
                    }
                    File.WriteAllText(sketchPath, sketchCode);
                }
                catch
                {
                    throw new Exception("스케치 파일 및 폴더를 생성할 수 없습니다.");
                }
                _loggingService.Log("스케치 파일 및 폴더 생성 완료.");
                _loggingService.Log("코드 컴파일 시작...");
                RunArduinoCli($"compile -b {fqbn} {sketchFolder}");
                _loggingService.Log("코드 컴파일 완료.");
                _loggingService.Log("코드 업로드 시작...");
                RunArduinoCli($"upload -p {port} -b {fqbn} {sketchFolder}");
                _loggingService.Log("코드 업로드 완료.");
                return true;
            });
        }

        static string RunArduinoCli(string arguments)
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
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            //Debug.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }

            return output;
        }

        static bool ExtractFqbnAndCore(string port, out string fqbn, out string core)
        {
            fqbn = "";
            core = "";
            int fqbnIndex = -1;
            int coreIndex = -1;

            string boardInfo = RunArduinoCli("board list");
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
                        fqbn = parts1[0];
                    }

                    string corePart = line.Substring(coreIndex).Trim();
                    string[] parts2 = corePart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts2.Length > 0)
                    {
                        core = parts2[0];
                    }

                    if (!string.IsNullOrEmpty(fqbn) && !string.IsNullOrEmpty(core)) return true;
                    else break;
                }
            }
            return false;
        }
    }
}
