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
using TelloController.Services;
using System.IO;
using Microsoft.Win32;
using TelloController.Enums;

namespace TelloController.ViewModel
{
    /// <summary>
    /// Viewmodel that drives the main window.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region Private Fields
        private Services.TelloController _connection;

        private TelloState _currentState;
        private string _log;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
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

            Task.Run(() =>
            {
                while (true)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        State = _currentState?.RawState.Replace(";", Environment.NewLine);
                        Result = _log;
                    }));
                    Thread.Sleep(200);
                }
            });

            _connection = new Services.TelloController();
            _connection.PropertyChanged += Connection_PropertyChanged;

            UpdateConnectionState();
        } 
        #endregion

        #region Commands
        public RelayCommand ConnectCommand => new RelayCommand(Connect);

        public RelayCommand<string> SendCommand => new RelayCommand<string>(Send);

        public RelayCommand OpenCommand => new RelayCommand(Open);

        public RelayCommand ExecuteCommandScriptCommand => new RelayCommand(ExecuteCommandScript);

        public RelayCommand StartRecordingCommand => new RelayCommand(StartRecording);

        public RelayCommand EndRecordingCommand => new RelayCommand(EndRecording);
        #endregion

        #region Properties
        public ObservableCollection<InputBinding> InputBindings { get; }

        private string _connectionText = "";
        public string ConnectionText
        {
            get { return _connectionText; }
            set { Set(ref _connectionText, value); }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { Set(ref _isConnected, value); }
        }

        private string _state = "";
        public string State
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
        private void StartRecording()
        {
            _connection.StartRecording();
        }

        private void EndRecording()
        {
            var states = _connection.EndRecording();
        }

        private void Connection_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Services.TelloController.IsListening))
            {
                UpdateConnectionState();
            }
        }

        private void UpdateConnectionState()
        {
            ConnectionText = _connection.IsListening ? "Disconnect" : "Connect";
            IsConnected = _connection.IsListening;
        }

        private void Connect()
        {
            _connection.Connect((response) => HandleCommandResponse(response), (state) => HandleState(state));
        }

        private void Send(string command)
        {
            AddLogEntry($"Sending -> {command}");
            _connection.SendCommand(command);
        }

        private CommandResponse SendCommandWithResponse(string command, int timeout)
        {
            AddLogEntry($"Sending -> {command}");
            return _connection.SendWithResponse(command, timeout);            
        }

        private void ExecuteCommandScript()
        {
            Task.Run(() =>
            {
                try
                {
                    const int DEFAULT_TIMEOUT = 10;
                    var timeout = DEFAULT_TIMEOUT;
                    if (!_connection.IsListening) return;
                    AddLogEntry($"* Executing script [Timeout = {timeout}]");
                    var lines = CommandScript.Split('\n');

                    foreach (var line in lines)
                    {
                        var entry = line.TrimEnd('\r');
                        if (entry.StartsWith("//"))
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
                            AddLogEntry($"* Set timeout to: {timeout}");
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
                    AddLogEntry($"** Error: {ex.Message}");
                }
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
                if (!_connection.IsListening) return;
                AddLogEntry("Reading command script");
                CommandScript = File.ReadAllText(filename);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Error: {ex.Message}");
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
            
            AddLogEntry($"Received <- {logMessage}");
        }

        private void HandleState(TelloState state)
        {
            _currentState = state;
        }

        private void AddLogEntry(string entry)
        {
            _log = $"{entry}{Environment.NewLine}{_log}";
        }
        #endregion
    }
}