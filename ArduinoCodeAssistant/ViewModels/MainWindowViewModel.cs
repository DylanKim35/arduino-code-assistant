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
        private readonly LoggingService _loggingService;
        private bool _isCommandRunning = false;
        public MainWindowViewModel(ArduinoService arduinoService,
            ArduinoInfo arduinoInfo,
            ChatService chatService,
            ChatResponse chatResponse,
            LoggingService loggingService)
        {
            _arduinoService = arduinoService;
            _arduinoInfo = arduinoInfo;
            _chatService = chatService;
            _chatResponse = chatResponse;
            _loggingService = loggingService;
        }

        #region DetectArduino

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

        private ICommand? _detectArduinoCommand;
        public ICommand DetectArduinoCommand =>
            _detectArduinoCommand ??= new RelayCommand<object>(async (o) =>
            {
                _isCommandRunning = true;
                CommandManager.InvalidateRequerySuggested();
                ArduinoPortStatus = "";
                ArduinoNameStatus = "";
                _loggingService.Log("기기 탐색 시작...");
                try
                {
                    if (await _arduinoService.DetectDeviceAsync())
                    {
                        _loggingService.Log($"작업 성공. (Name: {_arduinoInfo.Name}, Port: {_arduinoInfo.Port}, FQBN: {_arduinoInfo.Fqbn}, Core: {_arduinoInfo.Core})", LoggingService.LogLevel.Completed);
                    }
                }
                catch(Exception ex)
                {
                    _loggingService.Log("작업 실패: ", LoggingService.LogLevel.Error, ex);
                }
                finally
                {
                    ArduinoPortStatus = _arduinoInfo.Port ?? "";
                    ArduinoNameStatus = _arduinoInfo.Name ?? "";
                    _isCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => !_isCommandRunning);

        #endregion

        #region UploadCode

        // TODO: Replace exception throwing logic with logger
        private ICommand? _uploadCodeCommand;
        public ICommand UploadCodeCommand =>
            _uploadCodeCommand ??= new RelayCommand<object>(async (o) =>
            {
                _isCommandRunning = true;
                CommandManager.InvalidateRequerySuggested();
                _loggingService.Log("코드 컴파일 및 업로드 시작...");
                try
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                    var saveCodeToSketchFileTask = _arduinoService.UploadCodeAsync(ReceivedCode);
                    if (await Task.WhenAny(saveCodeToSketchFileTask, timeoutTask) == saveCodeToSketchFileTask)
                    {
                        if (await saveCodeToSketchFileTask)
                        {
                            _loggingService.Log("작업 성공.", LoggingService.LogLevel.Completed);
                        }
                    }
                    else
                    {
                        throw new Exception("대기시간을 초과하였습니다.");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Log("작업 실패: ", LoggingService.LogLevel.Error, ex);
                }
                finally
                {
                    _isCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => !_isCommandRunning);

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

        private ICommand? _sendChatMessageCommand;
        public ICommand SendChatMessageCommand =>
            _sendChatMessageCommand ??= new RelayCommand<object>(async (o) =>
            {
                _isCommandRunning = true;
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
                        _isCommandRunning = false;
                        CommandManager.InvalidateRequerySuggested();
                    }
                }
                else
                {
                    ReceivedCode = "[Error] No response";
                    ReceivedDescription = "[Error] No response";
                    _isCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => !_isCommandRunning);

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
