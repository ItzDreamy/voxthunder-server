using System;
using System.Linq;

namespace VoxelTanksServer
{
    public static class Commands
    {
        public static void ShowOnline()
        {
            Console.WriteLine($"Current online: {Server.OnlinePlayers} / {Server.MaxPlayers}");
        }

        public static void KickPlayer()
        {
            Console.Write("Player name: ");
            var nickname = Console.ReadLine();
            if (TryGetClient(nickname, out var client))
            {
                client.Disconnect("User kicked");
            }
        }

        public static void BanPlayer()
        {
            Console.Write("Player name: ");
            var nickname = Console.ReadLine();
            if (TryGetClient(nickname, out var client))
            {
                client.Disconnect("User banned");
            }
        }

        public static void StopServer()
        {
            Environment.Exit(0);
        }

        public static void ShowInfo()
        {
            //Info
        }
        
        private static bool TryGetClient(string? username, out Client client)
        {
            client = Server.Clients.Values.ToList().Find(c => c?.Username?.ToLower() == username.ToLower());
            Console.Write(client == null ? "Player not found\n" : null);
            return client != null;
        }
    }
}