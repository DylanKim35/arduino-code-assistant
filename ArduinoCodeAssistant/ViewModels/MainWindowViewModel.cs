using ArduinoCodeAssistant.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ArduinoCodeAssistant.ViewModels
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _devicePort;
        public string DevicePort
        {
            get => _devicePort;
            set
            {
                if (value != _devicePort)
                {
                    _devicePort = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _deviceName;
        public string DeviceName
        {
            get => _deviceName;
            set
            {
                if (value != _deviceName)
                {
                    _deviceName = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly ArduinoCommunication _arduinoCommunication;
        public MainWindowViewModel(ArduinoCommunication arduinoCommunication)
        {
            _arduinoCommunication = arduinoCommunication;
        }

        private ICommand? _detectArduinoButtonClickCommand;
        public ICommand DetectArduinoButtonClickCommand => 
            _detectArduinoButtonClickCommand ??= new RelayCommand<object>((o) =>
        {
            if (_arduinoCommunication.DetectDevice(out string devicePort, out string deviceName))
            {
                DevicePort = devicePort;
                DeviceName = deviceName;
            }
            else
            {
                DevicePort = "기기를 찾을 수 없습니다.";
                DeviceName = "-";
            }
        });

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
