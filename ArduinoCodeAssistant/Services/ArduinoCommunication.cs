using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCodeAssistant.Services
{
    internal class ArduinoCommunication
    {
        public bool DetectDevice(out string devicePort, out string deviceName)
        {
            devicePort = "";
            deviceName = "";

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
                        devicePort = queriedPort;
                        deviceName = queriedName;
                        return true;
                    }
                }
            }
            catch (ManagementException e)
            {
            }
            return false;
        }
    }
}
