using ArduinoCodeAssistant.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public ArduinoService(ArduinoInfo arduinoInfo)
        {
            _arduinoInfo = arduinoInfo;
        }

        public async Task<bool> DetectDeviceAsync()
        {
            return await Task.Run(() =>
            {
                _arduinoInfo.SetAllPropertiesToNull();

                if (!File.Exists(@"C:\Program Files\Arduino CLI\arduino-cli.exe"))
                {
                    throw new Exception("Arduino CLI 파일 찾기 오류");
                }

                ManagementScope connectionScope = new ManagementScope();
                SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);
                try
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        string? queriedPort = item["DeviceID"].ToString();
                        string? queriedName = item["Description"].ToString();

                        if (queriedPort != null && queriedName != null && queriedName.Contains("Arduino"))
                        {
                            _arduinoInfo.Port = queriedPort;
                            _arduinoInfo.Name = queriedName;


                            if (!ExtractFqbnAndCore(_arduinoInfo.Port, out string fqbn, out string core))
                            {
                                throw new Exception("fqbn, core 추출 오류");
                            }
                            _arduinoInfo.Fqbn = fqbn;
                            _arduinoInfo.Core = core;

                            if (!RunArduinoCli("core list").Contains(core))
                            {
                                RunArduinoCli($"core install {core}");
                            }

                            return true;
                        }
                    }
                }
                catch (ManagementException e)
                {
                }
                _arduinoInfo.Port = null;
                _arduinoInfo.Name = null;
                return false;
            });
        }

        public async Task<bool> UploadCodeAsync(string sketchCode)
        {
            return await Task.Run(() => {
                
                string port = _arduinoInfo.Port ?? throw new Exception("port는 null");
                string fqbn = _arduinoInfo.Fqbn ?? throw new Exception("fqbn는 null");


                string sketchFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempSketch");
                string sketchPath = Path.Combine(sketchFolder, "TempSketch.ino");

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
                    throw new Exception("스케치 파일 생성 오류");
                }

                RunArduinoCli($"compile --fqbn arduino:avr:uno \"{sketchFolder}\"");
                RunArduinoCli($"upload -p {port} --fqbn {fqbn} \"{sketchFolder}\"");

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

            Debug.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.WriteLine("Error: " + error);
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
