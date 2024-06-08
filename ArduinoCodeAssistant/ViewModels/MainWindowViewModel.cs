using ArduinoCodeAssistant.Models;
using ArduinoCodeAssistant.Services;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Xml.Linq;

namespace ArduinoCodeAssistant.ViewModels
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ArduinoService _arduinoService;
        private readonly ArduinoInfo _arduinoInfo;
        private readonly SavingService _savingService;
        private readonly ChatService _chatService;
        private readonly ChatResponse _chatResponse;
        private readonly LoggingService _loggingService;
        private readonly AudioRecorder _audioRecorder;
        private readonly WhisperService _whisperService;
        private bool _isCommandRunning = false;

        public MainWindowViewModel(ArduinoService arduinoService,
            ArduinoInfo arduinoInfo,
            SavingService savingService,
            ChatService chatService,
            ChatResponse chatResponse,
            LoggingService loggingService,
            AudioRecorder audioRecorder,
            WhisperService whisperService)
        {
            _arduinoService = arduinoService;
            _arduinoInfo = arduinoInfo;
            _savingService = savingService;
            _chatService = chatService;
            _chatResponse = chatResponse;
            _loggingService = loggingService;
            _audioRecorder = audioRecorder;
            _whisperService = whisperService;
            _audioRecorder = audioRecorder;
            _whisperService = whisperService;

            _textStatesDictionary = _savingService.TextStatesDictionary;
            TextStatesCollection = _savingService.TextStatesCollection;
            _savingService.InitializeTextStates();
            SelectedTextState = TextStatesCollection.Last();
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
                    if (await _arduinoService.UploadCodeAsync(GeneratedCode))
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

        private string _boardStatus;
        public string BoardStatus
        {
            get => _boardStatus;
            set
            {
                _boardStatus = value;
                OnPropertyChanged();
                if (SelectedTextState != null)
                    _savingService.SaveTextStatesAsync(SelectedTextState.Id, GeneratedTag, BoardStatus, RequestingMessage, GeneratedCode, GeneratedDescription).ConfigureAwait(false);
            }
        }

        private string _generatedCode;
        public string GeneratedCode
        {
            get => _generatedCode;
            set
            {
                if (value != _generatedCode)
                {
                    _generatedCode = value;
                    OnPropertyChanged();
                    if (SelectedTextState != null)
                        _savingService.SaveTextStatesAsync(SelectedTextState.Id, GeneratedTag, BoardStatus, RequestingMessage, GeneratedCode, GeneratedDescription).ConfigureAwait(false);
                }
            }
        }

        private string _generatedDescription;
        public string GeneratedDescription
        {
            get => _generatedDescription;
            set
            {
                if (value != _generatedDescription)
                {
                    _generatedDescription = value;
                    OnPropertyChanged();
                    if (SelectedTextState != null)
                        _savingService.SaveTextStatesAsync(SelectedTextState.Id, GeneratedTag, BoardStatus, RequestingMessage, GeneratedCode, GeneratedDescription).ConfigureAwait(false);
                }
            }
        }

        private string _generatedTag;
        public string GeneratedTag
        {
            get => _generatedTag;
            set
            {
                if (value != _generatedTag)
                {
                    _generatedTag = value;
                    OnPropertyChanged();
                    if (SelectedTextState != null)
                        _savingService.SaveTextStatesAsync(SelectedTextState.Id, GeneratedTag, BoardStatus, RequestingMessage, GeneratedCode, GeneratedDescription).ConfigureAwait(false);

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
                    if (SelectedTextState != null)
                        _savingService.SaveTextStatesAsync(SelectedTextState.Id, GeneratedTag, BoardStatus, RequestingMessage, GeneratedCode, GeneratedDescription).ConfigureAwait(false);
                }
            }
        }

        private ICommand? _sendChatMessageCommand;
        public ICommand SendChatMessageCommand =>
            _sendChatMessageCommand ??= new RelayCommand<object>(async (o) =>
            {
                _isCommandRunning = true;
                CommandManager.InvalidateRequerySuggested();
                AddEmptyTemplateCommand.Execute(null);
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
                        string tag = jsonResponse["tag"]?.ToString() ?? "[Error] No response";

                        _chatResponse.Code = code;
                        GeneratedCode = code;
                        _chatResponse.Description = description;
                        GeneratedDescription = description;
                        GeneratedTag = tag;
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

        private bool _isRecord = false;

        private string _recordButtonContent = "음성 인식 시작";
        public string RecordButtonContent
        {
            get { return _recordButtonContent; }
            set
            {
                _recordButtonContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand? _recordAudioCommand;

        public ICommand RecordAudioCommand =>
            _recordAudioCommand ??= new RelayCommand<object>(async (o) =>
            {
                if (!_isRecord)
                {
                    _isCommandRunning = true;
                    _isRecord = true;
                    CommandManager.InvalidateRequerySuggested();

                    _audioRecorder.StartRecording("tempRecord.mp3");

                    _loggingService.Log("음성 인식 시작", LoggingService.LogLevel.Info);
                    RecordButtonContent = "음성 인식 종료";
                }
                else
                {
                    _isRecord = false;

                    await _audioRecorder.StopRecordingAsync();
                    RequestingMessage = await _whisperService.GetTranscript(_audioRecorder.GetAudioFilePath());

                    _loggingService.Log("음성 인식 종료", LoggingService.LogLevel.Info);
                    RecordButtonContent = "음성 인식 시작";

                    _isCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }, (o) => !_isCommandRunning || _isRecord);

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

        #region TextStateIO

        private readonly Dictionary<Guid, TextState> _textStatesDictionary;
        public ObservableCollection<TextState> TextStatesCollection { get; }

        private TextState _selectedTextState;
        public TextState SelectedTextState
        {
            get { return _selectedTextState; }
            set
            {
                if (_selectedTextState != value)
                {
                    _selectedTextState = value;
                    OnPropertyChanged();
                    LoadTextState();
                }
            }
        }

        private void LoadTextState()
        {
            if (_textStatesDictionary != null && _textStatesDictionary.ContainsKey(SelectedTextState.Id))
            {
                var context = _textStatesDictionary[SelectedTextState.Id];
                _generatedTag = context.GeneratedTag;
                _boardStatus = context.BoardStatus;
                _requestingMessage = context.RequestingMessage;
                _generatedCode = context.GeneratedCode;
                _generatedDescription = context.GeneratedDescription;

                OnPropertyChanged(nameof(GeneratedTag));
                OnPropertyChanged(nameof(BoardStatus));
                OnPropertyChanged(nameof(RequestingMessage));
                OnPropertyChanged(nameof(GeneratedCode));
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private ICommand? _addEmptyTemplateCommand;
        public ICommand AddEmptyTemplateCommand =>
            _addEmptyTemplateCommand ??= new RelayCommand<object>((o) =>
            {
                var emptyTagItem = TextStatesCollection.FirstOrDefault(ts => ts.GeneratedTag == "");

                if (emptyTagItem != null)
                {
                    SelectedTextState = emptyTagItem;
                }
                else
                {
                    _savingService.AddEmptyTemplate();
                    SelectedTextState = TextStatesCollection.Last();
                    SetAllSavableTextBoxToEmpty();
                }
            });

        private ICommand? _removeTemplateCommand;
        public ICommand RemoveTemplateCommand =>
            _removeTemplateCommand ??= new RelayCommand<object>(async (o) =>
            {
                if (SelectedTextState != null)
                {
                    int index = TextStatesCollection.IndexOf(SelectedTextState);
                    if (index > 0)
                    {
                        SelectedTextState = TextStatesCollection[index - 1];
                    }
                    else if (TextStatesCollection.Count > 1)
                    {
                        SelectedTextState = TextStatesCollection[index + 1];
                    }
                    else
                    {
                        _savingService.AddEmptyTemplate();
                        SetAllSavableTextBoxToEmpty();
                        SelectedTextState = TextStatesCollection[index + 1];
                    }
                    await _savingService.RemoveTextStatesAsync(TextStatesCollection[index].Id);
                }
            });

        private void SetAllSavableTextBoxToEmpty()
        {
            _generatedTag = "";
            _boardStatus = "";
            _requestingMessage = "";
            _generatedCode = "";
            _generatedDescription = "";

            OnPropertyChanged(nameof(GeneratedTag));
            OnPropertyChanged(nameof(BoardStatus));
            OnPropertyChanged(nameof(RequestingMessage));
            OnPropertyChanged(nameof(GeneratedCode));
            OnPropertyChanged(nameof(GeneratedDescription));
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
