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
        private readonly ChatService _chatService;

        public ChatRequest InputMessage { get; set; } = new ChatRequest();

        public ChatResponse OutputMessage { get; set; } = new ChatResponse();
        public RelayCommand<object> SendChatMessageCommand { get; set; }

        public bool ChatFlag = true;

        private readonly ArduinoService _arduinoService;
        private readonly ArduinoInfo _arduinoInfo;
        public MainWindowViewModel(ArduinoService arduinoService, ChatService chatService, ArduinoInfo arduinoInfo)
        {
            _arduinoService = arduinoService;
            _chatService = chatService;
            _arduinoInfo = arduinoInfo;

            SendChatMessageCommand = new RelayCommand<object>(SendChatMessageAsync, SendChatButtonEnable);
        }

        private string _arduinoPortStatus;
        public string ArduinoPortStatus
        {
            get => _arduinoPortStatus;
            set
            {
                if (value != _arduinoPortStatus)
                {
                    _arduinoPortStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _arduinoNameStatus;
        public string ArduinoNameStatus
        {
            get => _arduinoNameStatus;
            set
            {
                if (value != _arduinoNameStatus)
                {
                    _arduinoNameStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _detectArduinoFlag = true;
        private ICommand? _detectArduinoCommand;
        public ICommand DetectArduinoCommand =>
            _detectArduinoCommand ??= new RelayCommand<object>(async (o) =>
            {
                _detectArduinoFlag = false;
                ArduinoPortStatus = "기기 탐색 중...";
                ArduinoNameStatus = "";

                var detectDeviceTask = _arduinoService.DetectDeviceAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

                if (await Task.WhenAny(detectDeviceTask, timeoutTask) == detectDeviceTask)
                {
                    if (detectDeviceTask.Result && _arduinoInfo.Port != null && _arduinoInfo.Name != null)
                    {
                        ArduinoPortStatus = _arduinoInfo.Port;
                        ArduinoNameStatus = _arduinoInfo.Name;
                    }
                    else
                    {
                        ArduinoPortStatus = "기기를 찾을 수 없습니다.";
                        ArduinoNameStatus = "-";
                    }
                }
                else
                {
                    ArduinoPortStatus = "기기 탐색 시간 초과";
                    ArduinoNameStatus = "-";
                }

                _detectArduinoFlag = true;
                CommandManager.InvalidateRequerySuggested();
            }, (o) => _detectArduinoFlag);


        private bool _compileAndUploadCodeFlag = true;
        private ICommand? _compileAndUploadCodeCommand;
        public ICommand CompileAndUploadCodeCommand =>
            _compileAndUploadCodeCommand ??= new RelayCommand<object>(async (o) =>
            {
                _compileAndUploadCodeFlag = false;
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

                var saveCodeToSketchFileTask = _arduinoService.UploadCodeAsync(OutputMessage.Code);
                if (await Task.WhenAny(saveCodeToSketchFileTask, timeoutTask) == saveCodeToSketchFileTask)
                {

                }
                else
                {
                    throw new Exception("타임아웃");
                }

                _compileAndUploadCodeFlag = true;
                CommandManager.InvalidateRequerySuggested();

            }, (o) => _compileAndUploadCodeFlag);



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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
