using ArduinoCodeAssistant.Models;
using ArduinoCodeAssistant.Services;
using Newtonsoft.Json.Linq;
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
        private readonly ChatService _chatService;

        public ChatRequest InputMessage { get; set; } = new ChatRequest();

        public ChatResponse OutputMessage { get; set; } = new ChatResponse();
        public RelayCommand<object> SendChatMessageCommand { get; set; }

        public bool ChatFlag = true;

        private readonly ArduinoCommunication _arduinoCommunication;
        public MainWindowViewModel(ArduinoCommunication arduinoCommunication, ChatService chatService)
        {
            _arduinoCommunication = arduinoCommunication;
            _chatService = chatService;

            SendChatMessageCommand = new RelayCommand<object>(SendChatMessageAsync, SendChatButtonEnable);
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

        public bool SendChatButtonEnable(object param)
        {
            return ChatFlag;
        }


        public async void SendChatMessageAsync(object param)
        {
            ChatFlag = false;
            OutputMessage.Code = "응답을 기다리는 중...";
            OutputMessage.Description = "응답을 기다리는 중...";

            var response = await _chatService.SendMessage(InputMessage);
            if (response != null)
            {
                string jsonString = response.Replace("```json", "").Replace("```", "");
                try
                {
                    var jsonResponse = JObject.Parse(jsonString);

                    OutputMessage.Code = jsonResponse["code"]?.ToString() ?? "[Error] No response";
                    OutputMessage.Description = jsonResponse["description"]?.ToString() ?? "[Error] No response";
                }
                catch (Exception ex)
                {
                    OutputMessage.Code = $"[Error] Invalid JSON: {ex.Message}";
                    OutputMessage.Description = $"[Error] {jsonString}";
                }
                finally
                {
                    ChatFlag = true;
                }
            }
            else
            {
                OutputMessage.Code = "[Error] No response";
                OutputMessage.Description = "[Error] No response";
                ChatFlag = true;
            }
        }
    }
}
