using ArduinoCodeAssistant.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

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
                ManagementScope connectionScope = new ManagementScope();
                SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

                try
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        string queriedPort = item["DeviceID"].ToString();
                        string queriedName = item["Description"].ToString();

                        if (queriedName.Contains("Arduino"))
                        {
                            _arduinoInfo.Port = queriedPort;
                            _arduinoInfo.Name = queriedName;
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
    }
}
