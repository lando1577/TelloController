using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelloController.Services
{
    public class ConnectionService : ViewModelBase
    {
        private const int PortReceive = 8890;
        private const int PortSend = 8889;
        private const string Address = "192.168.10.1";
        private UdpClient _commandClient;

        public ConnectionService()
        {

        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => Set(ref _isConnected, value);
        }

        public void Connect(Action<string> onResponseHandler)
        {
            if (IsConnected)
            {
                _commandClient?.Close();
                IsConnected = false;
            }
            else
            {
                _commandClient = new UdpClient(PortReceive);
                _commandClient.Connect(Address, PortSend);
                IsConnected = true;
                Task.Run(() =>
                {
                    Monitor(onResponseHandler);
                });
            }
        }

        private void Monitor(Action<string> onResponseHandler)
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (true)
                {
                    byte[] bytesReceived = _commandClient.Receive(ref remoteEP);               
                    string returnData = Encoding.UTF8.GetString(bytesReceived);
                    onResponseHandler.Invoke(returnData);
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
            }
        }

        public void Send(string command)
        {
            if (_commandClient == null) return;
            var commandBytes = Encoding.UTF8.GetBytes(command);
            _commandClient.Send(commandBytes, commandBytes.Length);
        }
    }
}
