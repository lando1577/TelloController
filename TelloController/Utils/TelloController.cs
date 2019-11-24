using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelloController.Enums;
using TelloController.Models;

namespace TelloController.Utils
{
    public class TelloController : ViewModelBase
    {
        #region Private Fields
        private int _portReceive;
        private int _portSend;
        private string _address;
        private UdpClient _telloClient;
        private CommandResponse _lastCommandResponse;
        private List<TelloState> _recordingData;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor with options for address and ports.
        /// </summary>
        /// <param name="address">Tello address to connect to.</param>
        /// <param name="portSend">Tello port to send on.</param>
        /// <param name="portReceive">Tello port to receive on.</param>
        public TelloController(string address, int portSend, int portReceive)
        {
            _address = address;
            _portSend = portSend;
            _portReceive = portReceive;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TelloController() : this("192.168.10.1", 8889, 8890)
        {
        }
        #endregion

        #region Public Properties
        private bool _isListening;
        public bool IsListening
        {
            get => _isListening;
            set
            {
                Set(ref _isListening, value);
                if (!IsListening) IsRecording = false;
                ResetStartTime();
            }
        }

        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            set => Set(ref _isRecording, value);
        }

        private DateTime _recordingStartTime;
        public DateTime RecordingStartTime 
        {
            get => _recordingStartTime; 
            private set => Set(ref _recordingStartTime, value); 
        }
        #endregion

        #region Public Methods
        public void Connect(Action<CommandResponse> onCommandResponseHandler, Action<TelloState> onStateHandler)
        {
            if (IsListening)
            {
                _telloClient?.Close();
                IsListening = false;
            }
            else
            {
                _telloClient = new UdpClient(_portReceive);
                _telloClient.Connect(_address, _portSend);

                IsListening = true;
                Task.Run(() =>
                {
                    Monitor(onCommandResponseHandler, onStateHandler);
                });
            }
        }

        public void StartStopRecording()
        {
            if (!IsListening)
            {
                IsRecording = false;
                return;
            }

            ResetStartTime();
            _recordingData = new List<TelloState>();
            IsRecording = !IsRecording;
        }

        public List<TelloState> EndRecording()
        {
            IsRecording = false;
            return _recordingData;
        }

        public void SendCommand(string command)
        {
            try
            {
                var commandBytes = Encoding.UTF8.GetBytes(command);
                _telloClient.Send(commandBytes, commandBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendCommand Error: {ex.Message}");
                EndConnection();
                throw;
            }            
        }

        public CommandResponse SendCommandWithResponse(string command, int timeout)
        {
            try
            {
                if (_telloClient == null) return new CommandResponse("Not connected", ResponseCode.NotConnected);

                _lastCommandResponse = null;

                SendCommand(command);

                int attemps = 0;
                while (_lastCommandResponse == null && attemps <= timeout)
                {
                    attemps++;
                    Thread.Sleep(1000);
                }

                if (attemps > timeout)
                {
                    throw new TimeoutException("Expected response was not received within the timeout");
                }

                return _lastCommandResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendCommandWithResponse Error: {ex.Message}");
                EndConnection();
                throw;
            }            
        } 
        #endregion

        #region Private Methods
        private void ResetStartTime()
        {
            RecordingStartTime = DateTime.Now;
        }

        private void Monitor(Action<CommandResponse> onCommandResponseHandler, Action<TelloState> onStateHandler)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (true)
                {
                    byte[] bytesReceived = _telloClient.Receive(ref endpoint);
                    string responseString = Encoding.UTF8.GetString(bytesReceived);
                    var currentTime = DateTime.Now;
                    
                    if (responseString.StartsWith("mid:"))
                    {
                        var state = ParseState(currentTime - RecordingStartTime, responseString);
                        if (_isRecording)
                        {
                            _recordingData.Add(state);
                        }
                        onStateHandler.Invoke(state);
                    }
                    else
                    {
                        var commandResponse = new CommandResponse(responseString, ParseResponseCode(responseString));
                        _lastCommandResponse = commandResponse;
                        onCommandResponseHandler.Invoke(commandResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Monitor Error: {ex.Message}");
                EndConnection();
            }
        }

        private void EndConnection()
        {
            _telloClient?.Close();
            IsListening = false;
        }

        private TelloState ParseState(TimeSpan timeSpan, string rawResponse)
        {            
            return TelloState.Parse((int) timeSpan.TotalMilliseconds, rawResponse);
        }

        private ResponseCode ParseResponseCode(string rawResponse)
        {
            var formatted = rawResponse.TrimEnd('\n').TrimEnd('\r');

            if (formatted == "ok")
            {
                return ResponseCode.Ok;
            }
            else if (formatted.StartsWith("error"))
            {
                return ResponseCode.Error;
            }
            else
            {
                return ResponseCode.Value;
            }
        } 
        #endregion
    }
}
