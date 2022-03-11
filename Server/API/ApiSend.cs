using System.Net;
using System.Net.NetworkInformation;

namespace VoxelTanksServer.API
{
    public class ApiSend
    {
        private static void SendTcpData(int toClient, Packet packet)
        {
            packet.WriteLength();
            ApiServer.Clients[toClient].Tcp.SendData(packet);
        }

        #region Packets

        public static void WelcomePacket(int toClient)
        {
            using (Packet packet = new((int) ServerApiPackets.Welcome))
            {
                int players = 0;

                foreach (var client in Server.Clients.Values)
                {
                    if (client.Tcp.Socket != null)
                    {
                        players++;
                    }
                }
                packet.Write(players);
                packet.Write(Server.MaxPlayers);
                SendTcpData(toClient, packet);
                
                if (players == Server.MaxPlayers)
                {
                    ApiServer.Clients[toClient].Disconnect();
                }
            }
        }
        
        public static void SendPlayersCount(int toClient, int playersCount, int maxPlayers)
        {
            using (Packet packet = new((int) ServerApiPackets.SendPlayersCount))
            {
                packet.Write(playersCount);
                packet.Write(maxPlayers);
                SendTcpData(toClient, packet);
            }
        }

        public static void SendServerState(int toClient, bool isOnline)
        {
            using (Packet packet = new((int) ServerApiPackets.SendServerState))
            {
                packet.Write(isOnline);
                
                SendTcpData(toClient, packet);
            }
        }

        public static void SendPing(int toClient, int pingOfClient)
        {
            using (Packet packet = new((int) ServerApiPackets.Ping))
            {
                Ping ping = new();
                PingReply reply = ping.Send((Server.Clients[pingOfClient].Tcp.Socket.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                packet.Write(reply.RoundtripTime);
                
                SendTcpData(toClient, packet);
            }
        }
        
        #endregion
    }
}