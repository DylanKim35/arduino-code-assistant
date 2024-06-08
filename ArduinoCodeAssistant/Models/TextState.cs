using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCodeAssistant.Models
{
    public class TextState : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _generatedTag = "";
        public string GeneratedTag
        {
            get { return _generatedTag; }
            set
            {
                _generatedTag = value;
                OnPropertyChanged();
            }
        }
        public string BoardStatus { get; set; } = "";
        public string RequestingMessage { get; set; } = "";
        public string GeneratedCode { get; set; } = "";
        public string GeneratedDescription { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
