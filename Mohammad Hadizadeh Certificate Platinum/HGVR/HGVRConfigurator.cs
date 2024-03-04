using System.Net.Sockets;
using Crestron.SimplSharp.CrestronWebSocketClient;

namespace Mohammad_Hadizadeh_Certificate_Platinum.HGVR
{
    public class HGVRConfigurator
    {
        private TransportTcpIp _client;
        private static readonly byte[] Header = new byte[] { 0x03, 0x08 };
        private static readonly byte[] Auth = new byte[] { 0x4D, 0x48, 0x34 };
        
        public HGVRConfigurator()
        {
            _client = new TransportTcpIp(Configurator.BuildingIpAddress, Configurator.BuildingPort, 1);
        }

        public void OpenWalls(ushort[] walls)
        {
            
        }

        public void TurnOnFans(ushort[] fans)
        {
            
        }
    }
}