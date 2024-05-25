using System.Collections.Generic;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class QuirkyTech
    {
        private static TransportTcpIp _client;
        private static CrestronQueue<string> _dataTxQueue;
        private static CEvent _dataTxEvent;
        //private static bool _dataTxRunning = false;
        
        private static Thread _sendDataThread;

        public QuirkyTech()
        {
            _client = new TransportTcpIp(Configurator.OrderIpAddress, Configurator.OrderPort, 1);
            _client.DataReceived += ClientOnDataReceived;
            _dataTxQueue = new CrestronQueue<string>(100);
            _dataTxEvent = new CEvent();
            
            _sendDataThread = new Thread(StartSendData, null);
            //CrestronInvoke.BeginInvoke(SendData, null);
        }

        private void ClientOnDataReceived(object sender, MessageEventArgs e)
        {
            CrestronConsole.PrintLine($"QuirkyTech Data received: {e.Message}");
        }
        
        public static void SendOrder(List<Retail> order)
        {
            foreach (var item in order)
            {
                var orderString = $"get item member {CardReader.MemberId} code {item.UPC}\x0D\x0A";
                _dataTxQueue.Enqueue(orderString);
            }

            _dataTxEvent.Set();
        }
        
        public static void StartRentalService(string spaceId)
        {
            if(_sendDataThread.ThreadState != Thread.eThreadStates.ThreadRunning)
                _sendDataThread.Start();
            
            var myStoreId = ControlSystem.MyStore.SPACE_ID;
            var myStoreFront = ControlSystem.StoreFronts[myStoreId];
            
            StringBuilder sb = new StringBuilder();
            sb.Append(myStoreId);

            if (myStoreFront != null)
            {
                if (myStoreFront.AssignedWorkSpaces != null)
                {
                    foreach (var workSpace in myStoreFront.AssignedWorkSpaces)
                    {
                        sb.Append(workSpace.SpaceId);
                    }
                }
            }
            
            var rentalService = $"rent open member {CardReader.MemberId} spaces {sb.ToString().ToLower()}\x0D\x0A";
            _dataTxQueue.Enqueue(rentalService);
            
            _dataTxEvent.Set();
        }
        
        public static void EndRentalService(string spaceId)
        {
            var myStoreId = ControlSystem.MyStore.SPACE_ID;
            var myStoreFront = ControlSystem.StoreFronts[myStoreId];
            
            StringBuilder sb = new StringBuilder();
            
            if (spaceId == myStoreId)
            {
                sb.Append(myStoreId);
                if (myStoreFront != null)
                {
                    if (myStoreFront.AssignedWorkSpaces != null)
                    {
                        foreach (var workSpace in myStoreFront.AssignedWorkSpaces)
                        {
                            sb.Append(workSpace.SpaceId);
                        }
                    }
                }
            }
            else
            {
                sb.Append(spaceId);
            }

            var rentalService = $"rent close member {CardReader.MemberId} spaces {sb.ToString().ToLower()}\x0D\x0A";
            _dataTxQueue.Enqueue(rentalService);
            
            _dataTxEvent.Set();
        }

        public static void SetDigitalSignageMessage(string spaceId, string message)
        {
            var digitalSignageService = $"display +{spaceId.ToLower()} \"{message}\"\x0D\x0A";
            _dataTxQueue.Enqueue(digitalSignageService);
            
            _dataTxEvent.Set();
        }
        
        public static void DeleteDigitalSignageMessage(string spaceId, string message)
        {
            var digitalSignageService = $"display -{spaceId.ToLower()}\x0D\x0A";
            _dataTxQueue.Enqueue(digitalSignageService);
            
            _dataTxEvent.Set();
        }
        
        private object StartSendData(object obj)
        {
            SendData(obj);
            return 1;
        }
        
        private void SendData(object obj)
        {
            while (true)
            {
                _dataTxEvent.Wait();
                CrestronConsole.PrintLine("---------------------------------- QuirkyTech Data Tx Event Set");
                if (_dataTxQueue.Count > 0)
                {
                    var temp = _dataTxQueue.Dequeue();
                    if (temp.Length > 0)
                    {
                        CrestronConsole.PrintLine($"QuirkyTech Data is being sent: {temp}");
                        _client.SendData(temp);
                        ClientOnDataReceived(null, new MessageEventArgs(){Message = "Data OK"});
                    }
                }
            }
        }
    }
}