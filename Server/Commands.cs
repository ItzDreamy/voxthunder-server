using System;
using System.Linq;

namespace VoxelTanksServer
{
    public class Commands
    {
        public static void ShowOnline()
        {
            Console.WriteLine($"Current online: {Server.OnlinePlayers} / {Server.MaxPlayers}");
        }

        public static void KickPlayer(string nickname)
        {
            if (TryGetClient(nickname, out var client))
            {
                client.Disconnect("User kicked");
            }
        }

        public static void BanPlayer(string nickname)
        {
            if (TryGetClient(nickname, out var client))
            {
                client.Disconnect("User banned");
            }
        }

        public static void StopServer()
        {
            Environment.Exit(0);
        }

        private static bool TryGetClient(string username, out Client client)
        {
            var foundedClient = Server.Clients.Values.ToList().Find(c => c?.Username?.ToLower() == username.ToLower());
            
            if (foundedClient == null)
            {
                Console.WriteLine("Player not found");
                client = null;
                return false;
            }

            client = foundedClient;
            return true;
        }
    }
}
