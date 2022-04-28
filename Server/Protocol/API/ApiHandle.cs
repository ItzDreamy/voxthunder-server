namespace VoxelTanksServer.Protocol.API;

public static class ApiHandle
{
    public static void GetPlayersCount(int fromClient, Packet packet)
    {
        ApiSend.SendPlayersCount(fromClient, Server.OnlinePlayers, Server.MaxPlayers);
    }

    public static void GetServerState(int fromClient, Packet packet)
    {
        bool isOnline = Server.IsOnline;
            
        ApiSend.SendServerState(fromClient, isOnline);
    }
}