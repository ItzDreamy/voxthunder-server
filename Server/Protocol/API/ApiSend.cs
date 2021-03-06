using System.Net;
using System.Net.NetworkInformation;

namespace VoxelTanksServer.Protocol.API;

public class ApiSend {
    private static void SendTcpData(int toClient, Packet packet) {
        packet.WriteLength();
        ApiServer.Clients[toClient].Tcp.SendData(packet);
    }

    #region Packets

    public static void WelcomePacket(int toClient) {
        using (Packet packet = new((int) ServerApiPackets.Welcome)) {
            var onlinePlayersCount = Server.OnlinePlayers;
            var maxPlayers = Server.MaxPlayers;
            packet.Write(onlinePlayersCount);
            packet.Write(maxPlayers);
            SendTcpData(toClient, packet);

            if (onlinePlayersCount == maxPlayers) ApiServer.Clients[toClient].Disconnect();
        }
    }

    public static void SendPlayersCount(int toClient, int playersCount, int maxPlayers) {
        using (Packet packet = new((int) ServerApiPackets.SendPlayersCount)) {
            packet.Write(playersCount);
            packet.Write(maxPlayers);
            SendTcpData(toClient, packet);
        }
    }

    public static void SendServerState(int toClient, bool isOnline) {
        using (Packet packet = new((int) ServerApiPackets.SendServerState)) {
            packet.Write(isOnline);

            SendTcpData(toClient, packet);
        }
    }

    public static void SendPing(int toClient, int pingOfClient) {
        using (Packet packet = new((int) ServerApiPackets.Ping)) {
            Ping ping = new();
            var reply = ping.Send((Server.Clients[pingOfClient].Tcp.Socket.Client.RemoteEndPoint as IPEndPoint).Address
                .ToString());
            packet.Write(reply.RoundtripTime);

            SendTcpData(toClient, packet);
        }
    }

    #endregion
}