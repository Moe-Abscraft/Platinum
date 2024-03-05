using System.Net.Sockets;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronWebSocketClient;

namespace Mohammad_Hadizadeh_Certificate_Platinum.HGVR
{
    public class HGVRConfigurator
    {
        private static TransportTcpIp _client;
        private static readonly byte[] Header = new byte[] { 0x03, 0x08 };
        private static readonly byte[] Auth = new byte[] { 0x4D, 0x48, 0x34 };
        
        public HGVRConfigurator()
        {
            _client = new TransportTcpIp(Configurator.BuildingIpAddress, Configurator.BuildingPort, 1);
        }

        public static void OpenWalls(ushort[] walls)
        {
            foreach (var wall in walls)
            {
                CrestronConsole.PrintLine($"Opening wall {wall}");
            }
        }

        public static void TurnOnFans(ushort[] fans)
        {
            foreach (var fan in fans)
            {
                CrestronConsole.PrintLine($"Turing on fan {fan}");
            }
        }
    }
}