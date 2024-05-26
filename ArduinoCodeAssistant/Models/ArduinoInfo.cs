using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCodeAssistant.Models
{
    public class ArduinoInfo : INotifyPropertyChanged
    {
        private string? _port;
        public string? Port
        {
            get => _port;
            set
            {
                if (value != _port)
                {
                    _port = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _name;
        public string? Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _fqbn;
        public string? Fqbn
        {
            get => _fqbn;
            set
            {
                if (value != _fqbn)
                {
                    _fqbn = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _core;
        public string? Core
        {
            get => _core;
            set
            {
                if (value != _core)
                {
                    _core = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SetAllPropertiesToNull()
        {
            Port = null;
            Name = null;
            Fqbn = null;
            Core = null;
        }

        //public bool ValidateProperties()
        //{
        //    return Port != null && Name != null && Fqbn != null && Core != null;
        //}

        public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    }
}
