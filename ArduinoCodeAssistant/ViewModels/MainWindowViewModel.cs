using ArduinoCodeAssistant.Models;
using ArduinoCodeAssistant.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
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
        private readonly MotionControlService _motionControlService;
        private bool _isCommandRunning;
        public bool IsCommandRunning
        {
            get => _isCommandRunning;
            set
            {
                if (value != _isCommandRunning)
                {
                    _isCommandRunning = value;
                    OnPropertyChanged();
                }    
            }
        }

        public MainWindowViewModel(ArduinoService arduinoService,
            ArduinoInfo arduinoInfo,
            SavingService savingService,
            ChatService chatService,
            ChatResponse chatResponse,
            LoggingService loggingService,
            AudioRecorder audioRecorder,
            WhisperService whisperService,
            MotionControlService motionControlService)
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
            _motionControlService = motionControlService;
            IsCommandRunning = false;

            #region TextStateIO
            _textStatesDictionary = _savingService.TextStatesDictionary;
            TextStatesCollection = _savingService.TextStatesCollection;
            _savingService.InitializeTextStates();
            SelectedTextState = TextStatesCollection.Last();
            #endregion

            #region MotionControl
            MotionControlPanelWidth = 640;
            MotionControlPanelHeight = 480;
            DeltaAngleThreshold = 5;
            SpeedRatioThreshold = 0.25;
            #endregion
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
                IsCommandRunning = true;
                CommandManager.InvalidateRequerySuggested();
                ArduinoPortStatus = "";
                ArduinoNameStatus = "";
                _arduinoInfo.SetAllPropertiesToNull();
                _arduinoService.CloseSerialPort();
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

                    if (_arduinoInfo.Port != null)
                        _arduinoService.OpenSerialPort(_arduinoInfo.Port);

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
                    IsCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => !IsCommandRunning);

        #endregion

        #region UploadCode

        private bool _allowAutoExecuteUpload;
        public bool AllowAutoExecuteUpload
        {
            get => _allowAutoExecuteUpload;
            set
            {
                _allowAutoExecuteUpload = value;
                OnPropertyChanged();
            }
        }

        private ICommand? _uploadCodeCommand;
        public ICommand UploadCodeCommand =>
            _uploadCodeCommand ??= new RelayCommand<object>(async (o) =>
            {
                IsCommandRunning = true;
                CommandManager.InvalidateRequerySuggested();
                _arduinoService.CloseSerialPort();
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

                    if (_arduinoInfo.Port != null)
                        _arduinoService.OpenSerialPort(_arduinoInfo.Port);

                    _loggingService.Log("작업 성공.", LoggingService.LogLevel.Completed);

                }
                catch (Exception ex)
                {
                    _loggingService.Log("작업 오류: ", LoggingService.LogLevel.Error, ex);
                }
                finally
                {
                    IsCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => !IsCommandRunning);

        #endregion

        #region SendChatMessage

        private bool _allowAutoExecuteGeneration;
        public bool AllowAutoExecuteGeneration
        {
            get => _allowAutoExecuteGeneration;
            set
            {
                _allowAutoExecuteGeneration = value;
                OnPropertyChanged();
            }
        }

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
                IsCommandRunning = true;
                CommandManager.InvalidateRequerySuggested();
                string prevBoardStatus = BoardStatus;
                string prevRequestingMessage = RequestingMessage;
                AddEmptyTemplateCommand.Execute(null);
                BoardStatus = prevBoardStatus;
                RequestingMessage = prevRequestingMessage;
                _loggingService.Log("응답을 기다리는 중...");
                try
                {
                    var response = await _chatService.SendMessage(RequestingMessage, BoardStatus);
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

                        if(AllowAutoExecuteUpload)
                        {
                            UploadCodeCommand.Execute(null);
                        }
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
                    IsCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }

            }, (o) => !IsCommandRunning);

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
                    IsCommandRunning = true;
                    _isRecord = true;
                    CommandManager.InvalidateRequerySuggested();

                    string prevBoardStatus = BoardStatus;
                    AddEmptyTemplateCommand.Execute(null);
                    BoardStatus = prevBoardStatus;

                    _audioRecorder.StartRecording("tempRecord.mp3");

                    _loggingService.Log("음성 인식 시작.", LoggingService.LogLevel.Info);
                    RecordButtonContent = "음성 인식 종료";
                }
                else
                {
                    _isRecord = false;

                    await _audioRecorder.StopRecordingAsync();
                    RequestingMessage = await _whisperService.GetTranscript(_audioRecorder.GetAudioFilePath());

                    _loggingService.Log("음성 인식 종료.", LoggingService.LogLevel.Info);
                    RecordButtonContent = "음성 인식 시작";

                    if (AllowAutoExecuteGeneration && !string.IsNullOrEmpty(RequestingMessage))
                    {
                        SendChatMessageCommand.Execute(null);
                    }

                    IsCommandRunning = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }, (o) => !IsCommandRunning || _isRecord);

        #endregion

        #region ClearTextBox

        private ICommand? _clearLogTextBoxCommand;
        public ICommand ClearLogTextBoxCommand =>
            _clearLogTextBoxCommand ??= new RelayCommand<object>((o) =>
            {
                _loggingService.ClearLogTextBox();
            });

        private ICommand? _clearSerialTextBoxCommand;
        public ICommand ClearSerialTextBoxCommand =>
            _clearSerialTextBoxCommand ??= new RelayCommand<object>((o) =>
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
                }

                SetAllSavableTextBoxToEmpty();
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

        #region MotionControl

        private object _motionControlPanelWidth;
        public object MotionControlPanelWidth
        {
            get { return _motionControlPanelWidth; }
            set
            {
                if (int.TryParse(value.ToString(), out int newValue))
                {
                    _motionControlPanelWidth = newValue;
                    OnPropertyChanged();
                }
            }
        }

        private object _motionControlPanelHeight;
        public object MotionControlPanelHeight
        {
            get { return _motionControlPanelHeight; }
            set
            {
                if (int.TryParse(value.ToString(), out int newValue))
                {
                    _motionControlPanelHeight = newValue;
                    OnPropertyChanged();
                }
            }
        }

        private double _deltaAngle;
        public double DeltaAngle
        {
            get { return _deltaAngle; }
            set
            {
                if (_deltaAngle != value)
                {
                    _deltaAngle = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _speedRatio;
        public double SpeedRatio
        {
            get { return _speedRatio; }
            set
            {
                if (_speedRatio != value)
                {
                    _speedRatio = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _deltaAngleThreshold;
        public double DeltaAngleThreshold
        {
            get { return _deltaAngleThreshold; }
            set
            {
                if (_deltaAngleThreshold != value)
                {
                    _deltaAngleThreshold = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _speedRatioThreshold;
        public double SpeedRatioThreshold
        {
            get { return _speedRatioThreshold; }
            set
            {
                if (_speedRatioThreshold != value)
                {
                    _speedRatioThreshold = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMotionCaptureRunning;
        public bool IsMotionCaptureRunning
        {
            get { return _isMotionCaptureRunning; }
            set
            {
                if (_isMotionCaptureRunning != value)
                {
                    _isMotionCaptureRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        private Process _pythonProcess;

        private ICommand? _showMotionControlPanelCommand;
        public ICommand ShowMotionControlPanelCommand =>
            _showMotionControlPanelCommand ??= new RelayCommand<object>(async (o) =>
            {
                if (_pythonProcess == null || _pythonProcess.HasExited)
                {
                    IsMotionCaptureRunning = true;
                    string pythonFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hand_tracking.py");
                    _pythonProcess = new Process();
                    _pythonProcess.StartInfo.FileName = $@"C:\Users\Administrator\AppData\Local\Programs\Python\Python312\python.exe";
                    _pythonProcess.StartInfo.Arguments = pythonFile +  $@" ""{MotionControlPanelWidth}"" ""{MotionControlPanelHeight}"" ""{DeltaAngleThreshold}"" ""{SpeedRatioThreshold}""";
                    _pythonProcess.StartInfo.UseShellExecute = false;
                    _pythonProcess.StartInfo.RedirectStandardOutput = true;
                    _pythonProcess.StartInfo.RedirectStandardError = true;
                    _pythonProcess.StartInfo.CreateNoWindow = true;

                    _pythonProcess.OutputDataReceived += PythonOutputHandler;
                    StringBuilder errorBuilder = new StringBuilder();
                    _pythonProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            // 오류 메시지를 수집합니다.
                            errorBuilder.AppendLine(e.Data);
                        }
                    };

                    _pythonProcess.Exited += (sender, e) =>
                    {
                        IsMotionCaptureRunning = false;
                        string errorMessage = errorBuilder.ToString();
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            _loggingService.Log("파이썬 실행 예외:\n" + errorMessage, LoggingService.LogLevel.Error);
                        }
                    };
                    _pythonProcess.EnableRaisingEvents = true;
                    _pythonProcess.Start();
                    _pythonProcess.BeginOutputReadLine();
                    _pythonProcess.BeginErrorReadLine();
                }
            });

        private void PythonOutputHandler(object sendingProcess, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {

                // 데이터를 ','로 분리하여 a와 b로 저장
                string[] parts = e.Data.Split(',');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], out double deltaAngle) && double.TryParse(parts[1], out double speedRatio))
                    {
                        // UI 스레드에서 변수에 값 할당
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 여기서 WPF의 변수에 값을 저장하거나 UI를 업데이트할 수 있음
                            // 예를 들어, 텍스트 박스에 출력하는 등의 작업을 수행할 수 있음
                            DeltaAngle = deltaAngle;
                            SpeedRatio = speedRatio;
                        });
                    }
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
