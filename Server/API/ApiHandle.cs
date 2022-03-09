namespace VoxelTanksServer.API
{
    public static class ApiHandle
    {
        public static void GetPlayersCount(int fromClient, Packet packet)
        {
            int players = 0;

            foreach (var client in Server.Clients.Values)
            {
                if (client.Tcp.Socket != null)
                {
                    players++;
                }
            }
            ApiSend.SendPlayersCount(fromClient, players, Server.MaxPlayers);
        }

        public static void GetServerState(int fromClient, Packet packet)
        {
            bool isOnline = Server.IsOnline;
            
            ApiSend.SendServerState(fromClient, isOnline);
        }
    }
}