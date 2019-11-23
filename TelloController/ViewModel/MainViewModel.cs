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

namespace TelloController.ViewModel
{
    /// <summary>
    /// Viewmodel that drives the main window.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region Private Fields
        private ConnectionService _connection;

        private bool _isConnected;
        private string _currentState;
        private string _currentResult; 
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ConnectionText = "Connect";

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

            //InputBindings = new ObservableCollection<InputBinding>();
            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(forward.Command)), new AnyKeyGesture(Key.W)));
            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(new ControlCommand("cw", "deg", 3600, 1, 10).Command)), new AnyKeyGesture(Key.A)));
            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(back.Command)), new AnyKeyGesture(Key.S)));
            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(new ControlCommand("ccw", "deg", 3600, 1, 10).Command)), new AnyKeyGesture(Key.D)));

            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(left.Command)), new AnyKeyGesture(Key.Q)));
            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(right.Command)), new AnyKeyGesture(Key.E)));

            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(up.Command)), new AnyKeyGesture(Key.R)));
            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(down.Command)), new AnyKeyGesture(Key.F)));

            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(takeoff.Command)), new AnyKeyGesture(Key.Z)));
            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(land.Command)), new AnyKeyGesture(Key.X)));

            //InputBindings.Add(new InputBinding(new RelayCommand(() => Send(emergency.Command)), new AnyKeyGesture(Key.Space)));

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
                        State = _currentState?.Replace(";", Environment.NewLine);
                        Result = _currentResult;
                    }));
                    Thread.Sleep(200);
                }
            });

            _connection = new ConnectionService();
            _connection.PropertyChanged += Connection_PropertyChanged;
        } 
        #endregion

        #region Commands
        public RelayCommand ConnectCommand => new RelayCommand(Connect);

        public RelayCommand<string> SendCommand => new RelayCommand<string>(Send); 
        #endregion

        #region Properties
        public ObservableCollection<InputBinding> InputBindings { get; }

        private string _connectionText = "";
        public string ConnectionText
        {
            get { return _connectionText; }
            set { Set(ref _connectionText, value); }
        }

        private string _state = "";
        public string State
        {
            get { return _state; }
            set { Set(ref _state, value); }
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
        private void Connection_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectionService.IsConnected))
            {
                UpdateConnectionState();
            }
        }

        private void UpdateConnectionState()
        {
            ConnectionText = _connection.IsConnected ? "Disconnect" : "Connect";
        }

        private void Connect()
        {
            _connection.Connect((response) => ParseResponse(response));
        }

        private void Send(string command)
        {
            _connection.Send(command);
        }

        private void ParseResponse(string response)
        {
            // check if it's a state response, or a sent command response
            if (response.StartsWith("mid:"))
            {
                _currentState = response;
            }
            else
            {
                _currentResult = response + Environment.NewLine + _currentResult;
            }
        }
        #endregion
    }
}