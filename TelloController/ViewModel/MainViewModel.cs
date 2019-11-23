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

namespace TelloController.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private const int PortReceive = 8890;
        private const int PortSend = 8889;
        private const string Address = "192.168.10.1";

        private UdpClient _server;
        private UdpClient _commandClient;

        private bool _isConnected;
        private string _currentState;
        private string _currentResult;

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
        }

        public RelayCommand ConnectCommand => new RelayCommand(Connect);

        public RelayCommand<string> SendCommand => new RelayCommand<string>(Send);

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

        public void Send(string command)
        {
            if (_commandClient == null) return;
            var commandBytes = Encoding.UTF8.GetBytes(command);
            _commandClient.Send(commandBytes, commandBytes.Length);
        }

        public void Connect()
        {
            if (_isConnected)
            {
                _server?.Close();
                _commandClient?.Close();
            }
            else
            {
                _commandClient = new UdpClient(PortReceive);
                _commandClient.Connect(Address, PortSend);
                Task.Run(() =>
                {
                    ConnectAndMonitor();
                });
            }
        }

        private void ConnectAndMonitor()
        {
            State = "Connecting";
            _isConnected = true;
            ConnectionText = "Disconnect";

            Console.WriteLine("Awaiting data from server...");
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);                       

            try
            {
                while (true)
                {
                    byte[] bytesReceived = _commandClient.Receive(ref remoteEP);
                    //Console.WriteLine($"Received {bytesReceived.Length} bytes from {remoteEP}");                   
                    string returnData = Encoding.UTF8.GetString(bytesReceived);
                    //Console.WriteLine(returnData);
                    if (returnData.StartsWith("mid:"))
                    {
                        _currentState = returnData;
                    }
                    else
                    {
                        _currentResult = returnData + Environment.NewLine + _currentResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionText = "Connect";
                State = ex.Message;
            }
        }
    }
}