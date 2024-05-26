using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronWebSocketClient;

namespace Mohammad_Hadizadeh_Certificate_Platinum.HGVR
{
    public class HGVRConfigurator
    {
        private static TransportTcpIp _client;
        private static readonly byte[] Header = new byte[] { 0x03, 0x08 };
        private static readonly byte[] Auth = new byte[] { 0x4D, 0x48, 0x34 };
        private static readonly byte[] Req = new byte[] { 0x01 };
        private static readonly byte[] Footer = new byte[] { 0x02, 0x0D };
        private static Dictionary<ushort, bool> _walls;
        private static Dictionary<ushort, bool> _fans;
        
        private readonly InquiryRequest _inquiryRequest;
        
        private bool _demo = true;
        
        public static event EventHandler<BuildingStatusArgs> BuildingStatusChanged = delegate { };
        private static void OnBuildingStatusChanged(BuildingStatusArgs e)
        {
            BuildingStatusChanged(null, e);
        }
        
        public event EventHandler<TemperatureArgs> TemperatureChanged = delegate { };
        protected virtual void OnTemperatureChanged(TemperatureArgs e)
        {
            TemperatureChanged(this, e);
        }
        
        public HGVRConfigurator(InquiryRequest inquiryRequest)
        {
            _inquiryRequest = inquiryRequest;
            _client = new TransportTcpIp(Configurator.BuildingIpAddress, Configurator.BuildingPort, 1, true);
            _walls = new Dictionary<ushort, bool>()
            {
                { 8, false },
                { 7, false },
                { 6, false },
                { 5, false },
                { 4, false },
                { 3, false },
                { 2, false },
                { 1, false },
                { 16, false },
                { 15, false },
                { 14, false },
                { 13, false },
                { 12, false },
                { 11, false },
                { 10, false },
                { 9, false },
            };
            _fans = new Dictionary<ushort, bool>()
            {
                { 8, false },
                { 7, false },
                { 6, false },
                { 5, false },
                { 4, false },
                { 3, false },
                { 2, false },
                { 1, false },
                { 16, false },
                { 15, false },
                { 14, false },
                { 13, false },
                { 12, false },
                { 11, false },
                { 10, false },
                { 9, false }
            };
            
            _client.DataReceivedBytes += ClientOnDataReceived;
            BuildingStatusChanged += HGVRConfigurator_BuildingStatusChanged;
        }

        private void ClientOnDataReceived(object sender, MessageBytesEventArgs e)
        {
            CrestronConsole.PrintLine($"Received data: {BitConverter.ToString(e.Message)}");
            var tempLS = e.Message[7];
            var tempMS = e.Message[8];
            var temp = new byte[] {tempMS, tempLS};
            var tempDecimal = Convert.ToInt32(BitConverter.ToString(temp).Replace("-", ""), 16);    
            var temperature = (tempDecimal / 100) - 100;
            CrestronConsole.PrintLine($"Temperature: {temperature}");
            OnTemperatureChanged(new TemperatureArgs() { Temperature = temperature.ToString() });
        }

        public static void UpdateWallStatus(ushort wall, bool status)
        {
            _walls[wall] = status;
        }
        
        public static void UpdateFanStatus(ushort fan, bool status)
        {
            _fans[fan] = status;
        }

        private void HGVRConfigurator_BuildingStatusChanged(object sender, BuildingStatusArgs e)
        {
            if(_demo)
                ClientOnDataReceived(null, new MessageBytesEventArgs() { Message = new byte[]
                {
                    0x03,
                    0x08,
                    0x01,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0xC8,
                    0x32,
                    0x00,
                    0x02,
                    0x0D
                }, Id = 1});

            _inquiryRequest.UpdateBuildingStatusRequest(ControlSystem.IpAddress, e.Walls, e.Fans);
            foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
            {
                _inquiryRequest.UpdateBuildingStatusRequest(storesIpAddress.ToString(), e.Walls, e.Fans);
            }
        }

        public static void OpenWalls(ushort[] walls)
        {

            foreach (var wall in walls)
            {
                CrestronConsole.PrintLine($"Opening wall {wall}");
                _walls[wall] = true;
            }
            
            var myStore = ControlSystem.StoreFronts[ControlSystem.MyStore.SPACE_ID];
            if (myStore.AssignedWorkSpaces == null || myStore.AssignedWorkSpaces.Count == 0)
            {
                CrestronConsole.PrintLine("No workspaces assigned to this store");
                return;
            }
            SendCommand();
        }
        
        public static void CloseWalls(ushort[] walls)
        {
            foreach (var wall in walls)
            {
                CrestronConsole.PrintLine($"Closing wall {wall}");
                _walls[wall] = false;
            }
            
            SendCommand();
        }

        public static void TurnOnFans(ushort[] fans)
        {
            foreach (var fan in fans)
            {
                CrestronConsole.PrintLine($"Turing on fan {fan}");
                _fans[fan] = true;
            }

            SendCommand();
        }
        
        public static void TurnOffFans(ushort[] fans)
        {
            foreach (var fan in fans)
            {
                CrestronConsole.PrintLine($"Turing off fan {fan}");
                _fans[fan] = false;
            }

            SendCommand();
        }

        private static void SendCommand()
        {
            // var wallsBytes = _walls.Select(wall => wall.Value ? (byte)1 : (byte)0).ToArray();
            // var fansBytes = _fans.Select(fan => fan.Value ? (byte)1 : (byte)0).ToArray();
            
            byte wallsBytesLS = 0;
            foreach (var wall in _walls.Where(w => w.Key <= 8))
            {
                if (wall.Value)
                {
                    wallsBytesLS |= (byte)(1 << (wall.Key - 1));
                }
            }
            
            byte wallsBytesMS = 0;
            foreach (var wall in _walls.Where(w => w.Key <= 16 && w.Key > 8))
            {
                if (wall.Value)
                {
                    wallsBytesMS |= (byte)(1 << (wall.Key - 1));
                }
            }
            
            byte fansBytesLS = 0;
            foreach (var fan in _fans.Where(f => f.Key <= 8))
            {
                if (fan.Value)
                {
                    fansBytesLS |= (byte)(1 << (fan.Key - 1));
                }
            }
            
            byte fansBytesMS = 0;
            foreach (var fan in _fans.Where(f => f.Key <= 16 && f.Key > 8))
            {
                if (fan.Value)
                {
                    fansBytesMS |= (byte)(1 << (fan.Key - 1));
                }
            }
            
            var command = new byte[Header.Length + Auth.Length + Req.Length + 4 + Footer.Length];
            Array.Copy(Header, 0, command, 0, Header.Length);
            Array.Copy(Auth, 0, command, Header.Length, Auth.Length);
            Array.Copy(Req, 0, command, Header.Length + Auth.Length, Req.Length);
            command[Header.Length + Auth.Length + Req.Length] = wallsBytesLS;
            command[Header.Length + Auth.Length + Req.Length + 1] = wallsBytesMS;
            command[Header.Length + Auth.Length + Req.Length + 2] = fansBytesLS;
            command[Header.Length + Auth.Length + Req.Length + 3] = fansBytesMS;
            Array.Copy(Footer, 0, command, Header.Length + Auth.Length + Req.Length + 4, Footer.Length);
            
            // print the command to the console in Hex format   
            CrestronConsole.PrintLine($"Sending command: [{BitConverter.ToString(command).Replace("-", "][")}]");
            OnBuildingStatusChanged(new BuildingStatusArgs() {Walls = _walls, Fans = _fans});
            _client.SendBytes(command);
        }
    }
    
    public class BuildingStatusArgs : EventArgs
    {
        public Dictionary<ushort, bool> Walls { get; set; }
        public Dictionary<ushort, bool> Fans { get; set; }
    }
    
    public class TemperatureArgs : EventArgs
    {
        public string Temperature { get; set; }
    }
}