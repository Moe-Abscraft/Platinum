using System;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class TransportTcpIpServer
    {
        private TCPServer _server;
        public event EventHandler<MessageEventArgs> ConnectionError = delegate { };
        protected virtual void OnConnectionError(MessageEventArgs e)
        {
            ConnectionError(this, e);
        }
        
        public event EventHandler<MessageEventArgs> DataReceived = delegate { };
        protected virtual void OnDataReceived(MessageEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }
        
        public TransportTcpIpServer()
        {
            _server = new TCPServer("0.0.0.0", 8000, 65535, EthernetAdapterType.EthernetLANAdapter);
            _server.SocketStatusChange += ServerOnSocketStatusChange;
            SocketErrorCodes err = _server.WaitForConnectionAsync(ServerConnectedCallback);
            // CrestronConsole.PrintLine($"Socket Error: {err.ToString()}");
        }

        private void ServerConnectedCallback(TCPServer mytcpserver, uint clientindex)
        {
            CrestronConsole.PrintLine($"Card Reader Server Client connected:{clientindex}");
            if (clientindex != 0)
            {
                _server.ReceiveDataAsync(clientindex, ServerReceiveDataCallback);
            }
            
            _server.WaitForConnectionAsync(ServerConnectedCallback);
        }

        private void ServerReceiveDataCallback(TCPServer mytcpserver, uint clientindex, int numberofbytesreceived)
        {
            try
            {
                if (numberofbytesreceived > 0)
                {
                    var data = new byte[numberofbytesreceived];
                    Array.Copy(mytcpserver.IncomingDataBuffer, data, numberofbytesreceived);
                    var message = Encoding.ASCII.GetString(data);
                    
                    if (message.All(char.IsDigit))
                    {
                        CrestronConsole.PrintLine($"Card Reader Server Data received:{message}");
                    }
                    else
                    {
                        var base64EncodedBytes = Convert.FromBase64String(message);
                        message = Encoding.ASCII.GetString(base64EncodedBytes);
                        CrestronConsole.PrintLine($"Card Reader Server Data received:{message}");
                    }
                    
                    OnDataReceived(new MessageEventArgs() { Message = message });
                    _server.SendDataAsync(clientindex, Encoding.ASCII.GetBytes($"OK"), 2, ServerDataSentCallback);
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in ServerReceiveDataCallback: {e.Message}");
            }
            finally
            {
                _server.ReceiveDataAsync(clientindex, ServerReceiveDataCallback);

            }
        }

        private void ServerDataSentCallback(TCPServer mytcpserver, uint clientindex, int numberofbytessent)
        {
            CrestronConsole.PrintLine($"Data sent:{numberofbytessent} bytes");
        }

        private void ServerOnSocketStatusChange(TCPServer mytcpserver, uint clientindex, SocketStatus serversocketstatus)
        {
            CrestronConsole.PrintLine($"Card Reader Server Socket status changed:{serversocketstatus}");
        }
        
        public void HandleLinkUp()
        {
            _server.HandleLinkUp();
        }

        public void HandleLinkDown()
        {
            _server.HandleLinkLoss();
        }
    }
}