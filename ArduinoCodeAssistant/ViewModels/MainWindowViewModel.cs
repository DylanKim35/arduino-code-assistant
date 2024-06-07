using ArduinoCodeAssistant.Models;
using ArduinoCodeAssistant.Services;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        private readonly AudioRecorder _audioRecorder;
        private readonly WhisperService _whisperService;
        private bool _isCommandRunning = false;

        public MainWindowViewModel(ArduinoService arduinoService,
            ArduinoInfo arduinoInfo,
            ChatService chatService,
            ChatResponse chatResponse,
            LoggingService loggingService,
            AudioRecorder audioRecorder,
            WhisperService whisperService)
        {
            _arduinoService = arduinoService;
            _arduinoInfo = arduinoInfo;
            _chatService = chatService;
            _chatResponse = chatResponse;
            _loggingService = loggingService;
            _audioRecorder = audioRecorder;
            _whisperService = whisperService;
            _audioRecorder = audioRecorder;
            _whisperService = whisperService;
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
                _arduinoInfo.SetAllPropertiesToNull();
                _loggingService.Log("작업 시작...");
                try
                {
                    if (await _arduinoService.DetectDeviceAsync())
                    {
                        _loggingService.Log($"기기 탐색 성공. (Name: {_arduinoInfo.Name}, Port: {_arduinoInfo.Port}, FQBN: {_arduinoInfo.Fqbn}, Core: {_arduinoInfo.Core})");
                    }
                    else
                    {
                        throw new Exception("Method _arduinoService.DetectDeviceAndOpenPortAsync returned false");
                    }
                    _arduinoService.OpenSerialPort(_arduinoInfo.Port, 9600);
                    _loggingService.Log("작업 성공.", LoggingService.LogLevel.Completed);
                }
                catch(Exception ex)
                {
                    _loggingService.Log("작업 오류: ", LoggingService.LogLevel.Error, ex);
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

        private ICommand? _uploadCodeCommand;
        public ICommand UploadCodeCommand =>
            _uploadCodeCommand ??= new RelayCommand<object>(async (o) =>
            {
                _isCommandRunning = true;
                CommandManager.InvalidateRequerySuggested();
                _loggingService.Log("작업 시작...");

                try
                {
                    if (await _arduinoService.UploadCodeAsync(ReceivedCode))
                    {
                        _loggingService.Log("코드 컴파일 및 업로드 성공.");
                    }
                    else
                    {
                        throw new Exception("Method _arduinoService.UploadCodeAsync returned false");
                    }

                    _loggingService.Log("작업 성공.", LoggingService.LogLevel.Completed);
                }
                catch (Exception ex)
                {
                    _loggingService.Log("작업 오류: ", LoggingService.LogLevel.Error, ex);
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
                _loggingService.Log("응답을 기다리는 중...");
                try
                {
                    var response = await _chatService.SendMessage(RequestingMessage);
                    if (response != null)
                    {
                        int startIndex = response.IndexOf("```json");
                        int endIndex = response.LastIndexOf("```");

                        string jsonString = response;
                        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                        {
                            jsonString = response.Substring(startIndex + 7, endIndex - (startIndex + 7));
                        }

                        var jsonResponse = JObject.Parse(jsonString);

                        string code = jsonResponse["code"]?.ToString() ?? "[Error] No response";
                        string description = jsonResponse["description"]?.ToString() ?? "[Error] No response";

                        _chatResponse.Code = code;
                        ReceivedCode = code;
                        _chatResponse.Description = description;
                        ReceivedDescription = description;
                        _loggingService.Log("작업 성공.", LoggingService.LogLevel.Completed);
                    }
                    else
                    {
                        throw new Exception("응답을 불러올 수 없습니다.");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Log("작업 오류: ", LoggingService.LogLevel.Error, ex);
                }
                finally
                {
                    _isCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => !_isCommandRunning);

        #endregion

        #region RecordAudio

        private bool _isRecord;

        private ICommand? _recordAudioCommand;

        public ICommand RecordAudioCommand =>
            _recordAudioCommand ??= new RelayCommand<object>(async (o) =>
            {
                _isCommandRunning = false;

                if (!_isRecord)
                {
                    _isRecord = true;
                    _audioRecorder.StartRecording("tempRecord.mp3");
                }
                else
                {
                    await _audioRecorder.StopRecordingAsync();
                    RequestingMessage = await _whisperService.GetTranscript(_audioRecorder.GetAudioFilePath());
                    _isRecord = false;
                }

                _isCommandRunning = true;

            }, (o) => true);

        #endregion

        #region ClearTextBox

        private ICommand? _clearLogTextBoxCommand;
        public ICommand ClearLogTextBoxCommand =>
            _clearLogTextBoxCommand ??= new RelayCommand<object>(async (o) =>
            {
                _loggingService.ClearLogTextBox();
            });

        private ICommand? _clearSerialTextBoxCommand;
        public ICommand ClearSerialTextBoxCommand =>
            _clearSerialTextBoxCommand ??= new RelayCommand<object>(async (o) =>
            {
                _loggingService.ClearSerialTextBox();
            });

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
