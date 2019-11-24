using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TelloController.Models;
using TelloController.Utils;
using System.IO;
using Microsoft.Win32;
using TelloController.Enums;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;

namespace TelloController.UI
{
    /// <summary>
    /// Viewmodel that drives the main window.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region Private Fields
        private Utils.TelloController _connection;

        private TelloState _currentState;        
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            LogEntries = new ObservableCollection<LogEntry>();
            LogEntriesCollectionView = CollectionViewSource.GetDefaultView(LogEntries);
            LogEntriesCollectionView.SortDescriptions.Add(new SortDescription(nameof(LogEntry.Time), ListSortDirection.Descending));          

            Action<ControlCommand> executeCommandFunc = control => Send(control.Command);

            var command = new ControlCommand("command");
            var takeoff = new ControlCommand("takeoff");
            var land = new ControlCommand("land");
            var emergency = new ControlCommand("emergency");
            var up = new ControlCommand("up", "cm", 500, 20, 20);
            var down = new ControlCommand("down", "cm", 500, 20, 20);
            var left = new ControlCommand("left", "cm", 500, 20, 20);
            var right = new ControlCommand("right", "cm", 500, 20, 20);
            var forward = new ControlCommand("forward", "cm", 500, 20, 20);
            var back = new ControlCommand("back", "cm", 500, 20, 20);
            var cw = new ControlCommand("cw", "deg", 3600, 1, 90);
            var ccw = new ControlCommand("ccw", "deg", 3600, 1, 90);
            var flip_l = new ControlCommand("flip l");
            var flip_r = new ControlCommand("flip r");
            var flip_f = new ControlCommand("flip f");
            var flip_b = new ControlCommand("flip b");

            ControlCommandsAvailable = new ObservableCollection<ControlCommand>
            {
                command,
                takeoff,
                land,
                emergency,
                up,
                down,
                left,
                right,
                forward,
                back,
                cw,
                ccw,
                flip_l,
                flip_r,
                flip_f,
                flip_b
            };

            // background thread for updating various states
            Task.Run(() =>
            {
                while (true)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        State = _currentState;
                        if (IsRecording)
                        {
                            RecordingDuration = DateTime.Now - RecordingStartTime;
                        }
                        else
                        {
                            RecordingDuration = null;
                        }
                    }));
                    Thread.Sleep(200);
                }
            });

            _connection = new Utils.TelloController();
            _connection.PropertyChanged += Connection_PropertyChanged;

            UpdateControllerState();
        } 
        #endregion

        #region Commands
        public RelayCommand ConnectCommand => new RelayCommand(Connect);

        public RelayCommand<string> SendCommand => new RelayCommand<string>(Send);

        public RelayCommand OpenCommand => new RelayCommand(Open);

        public RelayCommand ExecuteCommandScriptCommand => new RelayCommand(ExecuteCommandScript);

        public RelayCommand StartStopRecordingCommand => new RelayCommand(StartStopRecording);

        public RelayCommand EndRecordingCommand => new RelayCommand(EndRecording);
        #endregion

        #region Properties
        public bool IsListening => _connection?.IsListening ?? false;

        public bool IsRecording => _connection?.IsRecording ?? false;

        public DateTime RecordingStartTime => _connection?.RecordingStartTime ?? DateTime.MinValue;

        private TimeSpan? _recordingDuration;
        public TimeSpan? RecordingDuration
        {
            get => _recordingDuration;
            set => Set(ref _recordingDuration, value);
        }

        public ObservableCollection<LogEntry> LogEntries { get; }

        public ICollectionView LogEntriesCollectionView { get; }

        private string _connectionText = "";
        public string ConnectionText
        {
            get { return _connectionText; }
            set { Set(ref _connectionText, value); }
        }

        private string _recordingText = "";
        public string RecordingText
        {
            get { return _recordingText; }
            set { Set(ref _recordingText, value); }
        }

        private TelloState _state;
        public TelloState State
        {
            get { return _state; }
            set { Set(ref _state, value); }
        }

        private string _commandScript = "";
        public string CommandScript
        {
            get { return _commandScript; }
            set { Set(ref _commandScript, value); }
        }

        private string _result = "";
        public string Result
        {
            get { return _result; }
            set { Set(ref _result, value); }
        }

        private string _commandToSend = "command";
        public string CommandToSend
        {
            get { return _commandToSend; }
            set { Set(ref _commandToSend, value); }
        }        

        private ObservableCollection<ControlCommand> _controlCommandsAvailable;
        public ObservableCollection<ControlCommand> ControlCommandsAvailable
        {
            get => _controlCommandsAvailable;
            set => Set(ref _controlCommandsAvailable, value);
        }

        public List<string> StateCommandsAvailable => new List<string>
        {
            "speed?",
            "battery?",
            "time?",
            "height?",
            "temp?",
            "attitude?",
            "baro?",
            "acceleration?",
            "tof?",
            "wifi?"
        }; 
        #endregion

        #region Private Methods
        private void StartStopRecording()
        {
            AddLogEntry(LogEntryType.Local, IsRecording ? "Stopping recording" : "Started recording");
            _connection.StartStopRecording();

        }

        private void EndRecording()
        {
            var states = _connection.EndRecording();
            var exportDirectory = AppDomain.CurrentDomain.BaseDirectory + $"Export";
            var exportFile = $"tello_{_connection.RecordingStartTime.ToString("s").Replace(":",string.Empty)}.csv";
            var exportFullPath = $"{exportDirectory}\\{exportFile}";
            if (!Directory.Exists(exportDirectory))
            {
                Directory.CreateDirectory(exportDirectory);
            }
            CsvUtil.GenerateCsv(exportFullPath, states);
            AddLogEntry(LogEntryType.Local, $"Ended recording -> {exportFile}");
            Process.Start(exportDirectory);
        }

        private void Connection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Utils.TelloController.IsListening):
                    UpdateControllerState();
                    RaisePropertyChanged(e.PropertyName);
                    break;
                case nameof(Utils.TelloController.RecordingStartTime):
                    RaisePropertyChanged(e.PropertyName);
                    break;
                case nameof(Utils.TelloController.IsRecording):
                    UpdateControllerState();
                    RaisePropertyChanged(e.PropertyName);
                    break;
            }
        }

        private void UpdateControllerState()
        {
            ConnectionText = IsListening ? "Disconnect" : "Connect";
            RecordingText = IsRecording ? "Stop Recording" : "Start Recording";
        }

        private void Connect()
        {
            AddLogEntry(LogEntryType.Local, IsListening ? "Ending connection" : "Establishing connection");
            _connection.Connect((response) => HandleCommandResponse(response), (state) => HandleState(state));
        }

        private void Send(string command)
        {
            try
            {
                AddLogEntry(LogEntryType.Send, $"{command}");
                _connection.SendCommand(command);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private CommandResponse SendCommandWithResponse(string command, int timeout)
        {
            AddLogEntry(LogEntryType.Send, $"{command}");
            return _connection.SendCommandWithResponse(command, timeout);
        }

        private void ExecuteCommandScript()
        {
            Task.Run(() =>
            {
                try
                {
                    const int DEFAULT_TIMEOUT = 10;
                    var timeout = DEFAULT_TIMEOUT;
                    var lines = CommandScript.Split('\n');
                    if (!_connection.IsListening || lines.Length == 0) return;
                    AddLogEntry(LogEntryType.Space, string.Empty);
                    AddLogEntry(LogEntryType.Local, $"Executing script [Timeout = {timeout}]");

                    foreach (var line in lines)
                    {
                        var entry = line.TrimEnd('\r');
                        if (entry.StartsWith("//") || string.IsNullOrEmpty(entry))
                        {
                            continue;
                        }
                        if (entry.StartsWith("delay"))
                        {
                            var delayParts = entry.Split(' ');
                            var seconds = int.Parse(delayParts[1]);
                            Thread.Sleep(seconds * 1000);
                        }
                        else if (entry.StartsWith("set timeout"))
                        {
                            var timeoutParts = entry.Split(' ');
                            timeout = int.Parse(timeoutParts[2]);
                            AddLogEntry(LogEntryType.Local, $"Set timeout to: {timeout}");
                        }
                        else
                        {
                            var response = SendCommandWithResponse(entry, timeout);
                            if (response.Code != ResponseCode.Ok)
                            {
                                // something unexpected happened, end the script and emergency land
                                Send("emergency");
                                throw new Exception("Expected response not received.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLogEntry(LogEntryType.Error, $"{ex.Message}");
                }
                AddLogEntry(LogEntryType.Local, "Script finished");
                AddLogEntry(LogEntryType.Space, string.Empty);
            });
        }

        private void Open()
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.DefaultExt = ".command";
                dialog.Filter = "Command Text Files (*.command)|*.command";
                dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Resources";
                var result = dialog.ShowDialog();

                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    var filename = dialog.FileName;
                    ParseCommandFile(filename);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while opening file: {ex.Message}");
            }
            
        }

        private void ParseCommandFile(string filename)
        {
            try
            {
                AddLogEntry(new LogEntry(LogEntryType.Local, "Reading command script"));
                CommandScript = File.ReadAllText(filename);
            }
            catch (Exception ex)
            {
                AddLogEntry(new LogEntry(LogEntryType.Error, $"{ex.Message}"));
            }
        }

        private void HandleCommandResponse(CommandResponse response)
        {
            string logMessage;
            switch (response.Code)
            {
                case ResponseCode.NotConnected:
                    logMessage = "Not Connected";
                    break;
                case ResponseCode.Value:
                    logMessage = response.ActualResponse.Replace("\n", string.Empty).Replace("\r", string.Empty);
                    break;
                case ResponseCode.Ok:
                    logMessage = "OK";
                    break;
                case ResponseCode.Error:
                    logMessage = "ERROR";
                    break;
                default:
                    logMessage = "UNKNOWN_RESPONSE_CODE";
                    break;
            }
            
            AddLogEntry(LogEntryType.Receive, $"{logMessage}");
        }

        private void HandleState(TelloState state)
        {
            _currentState = state;
        }

        private void AddLogEntry(LogEntryType type, string message, DateTime? time = null)
        {
            AddLogEntry(new LogEntry(type, message, time));
        }

        private void AddLogEntry(LogEntry entry)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => 
            {
                LogEntries.Add(entry);
            }));
        }
        #endregion
    }
}