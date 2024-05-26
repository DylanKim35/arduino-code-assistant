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
        private readonly ArduinoService _arduinoService;
        private readonly ArduinoInfo _arduinoInfo;
        private readonly ChatService _chatService;
        private readonly ChatResponse _chatResponse;

        public MainWindowViewModel(ArduinoService arduinoService,
            ArduinoInfo arduinoInfo,
            ChatService chatService,
            ChatResponse chatResponse)
        {
            _arduinoService = arduinoService;
            _arduinoInfo = arduinoInfo;
            _chatService = chatService;
            _chatResponse = chatResponse;
        }

        #region DetectArduino

        // TODO: Delete this property
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

        // TODO: Delete this property
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
                CommandManager.InvalidateRequerySuggested();
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

        #endregion

        #region UploadCode

        // TODO: Replace exception throwing logic with logger
        private bool _uploadCodeFlag = true;
        private ICommand? _uploadCodeCommand;
        public ICommand UploadCodeCommand =>
            _uploadCodeCommand ??= new RelayCommand<object>(async (o) =>
            {
                _uploadCodeFlag = false;
                CommandManager.InvalidateRequerySuggested();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

                var saveCodeToSketchFileTask = _arduinoService.UploadCodeAsync(ReceivedCode);
                if (await Task.WhenAny(saveCodeToSketchFileTask, timeoutTask) == saveCodeToSketchFileTask)
                {

                }
                else
                {
                    throw new Exception("기기 탐색 시간 초과");
                }

                _uploadCodeFlag = true;
                CommandManager.InvalidateRequerySuggested();

            }, (o) => _uploadCodeFlag);

        #endregion

        #region SendChatMessage

        private string _receivedCode;
        public string ReceivedCode
        {
            get => _receivedCode;
            set
            {
                if (value != _receivedCode)
                {
                    _receivedCode = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _receivedDescription;
        public string ReceivedDescription
        {
            get => _receivedDescription;
            set
            {
                if (value != _receivedDescription)
                {
                    _receivedDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _requestingMessage;
        public string RequestingMessage
        {
            get => _requestingMessage;
            set
            {
                if (value != _requestingMessage)
                {
                    _requestingMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool _chatFlag = true;
        private ICommand? _sendChatMessageCommand;
        public ICommand SendChatMessageCommand =>
            _sendChatMessageCommand ??= new RelayCommand<object>(async (o) =>
            {
                _chatFlag = false;
                CommandManager.InvalidateRequerySuggested();
                ReceivedCode = "응답을 기다리는 중...";
                ReceivedDescription = "응답을 기다리는 중...";

                // _chatRequest.Message는 기존에 의도하셨던 대로 SendMessage 내부에서 RequestingMessage로 수정되도록 만들었습니다. @김영민
                var response = await _chatService.SendMessage(RequestingMessage);
                if (response != null)
                {
                    string jsonString = response.Replace("```json", "").Replace("```", "");
                    try
                    {
                        var jsonResponse = JObject.Parse(jsonString);

                        string code = jsonResponse["code"]?.ToString() ?? "[Error] No response";
                        string description = jsonResponse["description"]?.ToString() ?? "[Error] No response";

                        _chatResponse.Code = code;
                        ReceivedCode = code;
                        _chatResponse.Description = description;
                        ReceivedDescription = description;
                    }
                    catch (Exception ex)
                    {
                        ReceivedCode = $"[Error] Invalid JSON: {ex.Message}";
                        ReceivedDescription = $"[Error] {jsonString}";
                    }
                    finally
                    {
                        _chatFlag = true;
                        CommandManager.InvalidateRequerySuggested();
                    }
                }
                else
                {
                    ReceivedCode = "[Error] No response";
                    ReceivedDescription = "[Error] No response";
                    _chatFlag = true;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => _chatFlag);

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
