using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Independentsoft.Exchange;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class QuirkyTech
    {
        private static TransportTcpIp _client;
        private static CrestronQueue<string> _dataTxQueue;
        private static CEvent _dataTxEvent;
        private static bool _dataTxRunning = false;

        public QuirkyTech()
        {
            _client = new TransportTcpIp(Configurator.OrderIpAddress, Configurator.OrderPort, 1);
            _client.DataReceived += ClientOnDataReceived;
            _dataTxQueue = new CrestronQueue<string>(100);
            _dataTxEvent = new CEvent(false, false);
            
            CrestronInvoke.BeginInvoke(SendData, null);
        }

        private void ClientOnDataReceived(object sender, MessageEventArgs e)
        {
            _dataTxEvent.Set();
        }
        
        public static void SendOrder(List<Retail> order)
        {
            foreach (var item in order)
            {
                var orderString = $"get item member {CardReader.MemberId} code {item.UPC}\r\n";
                _dataTxQueue.Enqueue(orderString);
            }

            if (_dataTxRunning) return;
            _dataTxEvent.Set();
            _dataTxRunning = true;
        }
        
        public static void StartRentalService(string spaceId)
        {
            var rentalService = $"rent open member {CardReader.MemberId} spaces {spaceId}\r\n";
            _dataTxQueue.Enqueue(rentalService);
            
            if (_dataTxRunning) return;
            _dataTxEvent.Set();
            _dataTxRunning = true;
        }
        
        public static void EndRentalService(string spaceId)
        {
            var rentalService = $"rent close member {CardReader.MemberId} spaces {spaceId}\r\n";
            _dataTxQueue.Enqueue(rentalService);
            
            if (_dataTxRunning) return;
            _dataTxEvent.Set();
            _dataTxRunning = true;
        }

        public static void SetDigitalSignageMessage(string spaceId, string message)
        {
            var digitalSignageService = $"display +{spaceId.ToLower()} \"{spaceId}\"\r\n";
            _dataTxQueue.Enqueue(digitalSignageService);
            
            if (_dataTxRunning) return;
            _dataTxEvent.Set();
            _dataTxRunning = true;
        }
        
        public static void DeleteDigitalSignageMessage(string spaceId, string message)
        {
            var digitalSignageService = $"display -{spaceId.ToLower()}\r\n";
            _dataTxQueue.Enqueue(digitalSignageService);
            
            if (_dataTxRunning) return;
            _dataTxEvent.Set();
            _dataTxRunning = true;
        }
        
        private void SendData(object obj)
        {
            while (true)
            {
                _dataTxEvent.Wait();
                if (_dataTxQueue.Count > 0)
                {
                    var temp = _dataTxQueue.Dequeue();
                    if (temp.Length > 0)
                    {
                        CrestronConsole.PrintLine($"Data is being sent: {temp}");
                        _client.SendData(temp);
                        ClientOnDataReceived(null, new MessageEventArgs(){Message = "Data OK"});
                    }
                }
            }
        }
    }
}