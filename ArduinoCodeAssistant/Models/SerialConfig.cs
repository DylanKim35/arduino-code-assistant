using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCodeAssistant.Models
{
    public class SerialConfig : INotifyPropertyChanged
    {
        private SerialPort? _serialPort;
        public SerialPort? SerialPort
        {
            get => _serialPort;
            set
            {
                if (value != _serialPort)
                {
                    _serialPort = value;
                    OnPropertyChanged();
                }
            }
        }

        //private int _baudRate;
        //public int BaudRate
        //{
        //    get => _baudRate;
        //    set
        //    {
        //        if (value != _baudRate)
        //        {
        //            _baudRate = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
