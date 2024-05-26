using System;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class TransportTcpIp
    {
        private uint _id;
        public uint ID
        {
            set => _id = value;
            get => _id;
        }
        private int _port;
        public int Port
        {
            set => _port = value;
            get => _port;
        }
        private string _address;
        public string Address
        {
            set => _address = value;
            get => _address;
        }
        
        private TCPClient _client;

        public bool IsConnected;

        private CTimer _connectionTimer;

        public event EventHandler<MessageEventArgs> ConnectionError = delegate { };
        protected virtual void OnConnectionError(MessageEventArgs e)
        {
            ConnectionError(this, e);
        }

        public event EventHandler<MessageEventArgs> DataReceived = delegate { };
        protected virtual void OnDataReceived(MessageEventArgs e)
        {
            DataReceived(this, e);
        }
        
        public event EventHandler<MessageBytesEventArgs> DataReceivedBytes = delegate { };
        protected virtual void OnDataReceivedBytes(MessageBytesEventArgs e)
        {
            DataReceivedBytes(this, e);
        }

        private CrestronQueue<string> _dataTxQueue;
        private readonly CEvent _dataTxEvent;
        private readonly CTimer _dataTxTimer;

        public TransportTcpIp(string address, int port, uint id, bool autoConnect)
        {
            Address = address;
            Port = port;
            ID = id;

            _dataTxEvent = new CEvent(true, false);
            _dataTxTimer = new CTimer(DataTxTimerCallback, null, 0, Timeout.Infinite);

            if(autoConnect)
                Connect(address, port);
        }
        
        public void Connect()
        {
            Connect(Address, Port);
        }

        private void DataTxTimerCallback(object o)
        {
            DataTxDequeue(o);
        }

        private void Connect(string address, int port)
        {
            if (string.IsNullOrEmpty(address) && port <= 0)
            {
                OnConnectionError(new MessageEventArgs()
                    { Message = "Port can not be 0, Address can not be empty", Id = _id });
            }

            if (_client != null)
            {
                if (_client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
                {
                    _client.DisconnectFromServer();
                    _client.SocketStatusChange -= ClientOnSocketStatusChange;
                }
                _client.SocketStatusChange += ClientOnSocketStatusChange;
                
                if (_connectionTimer != null)
                {
                    _connectionTimer.Dispose();
                }
                _connectionTimer = new CTimer(ConnectTimerCallback, null, 1000, 30000);
                _client.ConnectToServer();
                return;
                // _client.Dispose();
            }

            if (_connectionTimer != null)
            {
                _connectionTimer.Dispose();
            }

            _client = new TCPClient(address, port, 10 * 1024);
            _client.SocketStatusChange += ClientOnSocketStatusChange;
            _connectionTimer = new CTimer(ConnectTimerCallback, null, 1000, 30000);
        }

        public void Disconnect()
        {
            try
            {
                OnConnectionError(new MessageEventArgs() { Message = "Disconnecting...", Id = _id });
                _connectionTimer.Stop();
                _connectionTimer.Dispose();
                _client.DisconnectFromServer();
            }
            catch (Exception e)
            {
                OnConnectionError(new MessageEventArgs() { Message = $"Error disconnecting: {e.Message}", Id = _id });
            }
        }

        public void SendData(string data)
        {
            if (_client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                if (data.Length > 0)
                {
                    _dataTxQueue.Enqueue(data);
                    _dataTxTimer.Reset(500);   
                }
                // _dataTxEvent.Set();
            }
        }

        private void ClientOnSocketStatusChange(TCPClient mytcpclient, SocketStatus clientsocketstatus)
        {
            if (clientsocketstatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                IsConnected = true;
                _connectionTimer.Stop();
                OnConnectionError(new MessageEventArgs() { Message = $"{Address} Connected", Id = _id });

                if (_dataTxQueue != null)
                {
                    _dataTxQueue.Dispose();
                }

                _dataTxQueue = new CrestronQueue<string>();
                // CrestronInvoke.BeginInvoke(DataTxDequeue);
                // SendData("Hello Moe!!");
            }
            else
            {
                IsConnected = false;

                OnConnectionError(new MessageEventArgs() { Message = $"{Address} Disconnected", Id = _id });
                _connectionTimer?.Reset(1000, 30000);
            }
        }

        private void ConnectTimerCallback(object o)
        {
            OnConnectionError(new MessageEventArgs() { Message = "Connecting...", Id = _id });
            if (_client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                _client.ConnectToServerAsync(ConnectCallback);
            }
        }

        private void ConnectCallback(TCPClient client)
        {
            if (client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                client.ReceiveDataAsync(ReceiveDataCallback);
            }
        }

        private void ReceiveDataCallback(TCPClient client, int numOfBytes)
        {
            try
            {
                var data = new string(client.IncomingDataBuffer.Take(numOfBytes).Select(b => (char)b).ToArray());
                OnDataReceived(new MessageEventArgs() { Message = data, Id = _id });
                OnDataReceivedBytes(new MessageBytesEventArgs() { Message = client.IncomingDataBuffer.Take(numOfBytes).ToArray(), Id = _id });

                client.ReceiveDataAsync(ReceiveDataCallback);
            }
            catch (Exception e)
            {
                OnConnectionError(new MessageEventArgs() { Message = $"Error receiving data {e.Message}", Id = _id });
            }
        }

        private void DataTxDequeue(object o)
        {
            //while (true)
            //{
                try
                {
                    // _dataTxEvent.Wait();
                    //if (_dataTxQueue.Count <= 0) continue;
                    string temp = _dataTxQueue.Dequeue();
                    if (temp.Length > 0)
                    {
                        TransmitData(temp);
                        // CrestronConsole.PrintLine($"Transmitting: {Address} ::: {temp.Trim('\x0D')}");
                    }
                }
                catch (Exception e)
                {
                    OnConnectionError(new MessageEventArgs() { Message = "Error dequeue send data", Id = _id });
                }
            //}
        }

        private void TransmitData(string data)
        {
            if (_client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED) return;
            byte[] dayaBytes = Encoding.ASCII.GetBytes(data);
            if (_client.SendData(dayaBytes, data.Length) == SocketErrorCodes.SOCKET_OK)
            {
                // _dataTxEvent.Set();
                _dataTxTimer.Reset(500);
            }
        }

        public void SendBytes(byte[] data)
        {
            if (_client.SendData(data, data.Length) == SocketErrorCodes.SOCKET_OK)
            {
                // _dataTxEvent.Set();
                _dataTxTimer.Reset(500);
            }
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public uint Id { get; set; }
    }
    
    public class MessageBytesEventArgs : EventArgs
    {
        public byte[] Message { get; set; }
        public uint Id { get; set; }
    }
}