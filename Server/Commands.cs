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
            var client = Server.Clients.Values.ToList().Find(c => c?.Username?.ToLower() == nickname.ToLower());

            if (CheckPlayerForExist(client))
            {
                client.Disconnect("User banned");
            }
        }

        public static void BanPlayer(string nickname)
        {
            var client = Server.Clients.Values.ToList().Find(c => c?.Username?.ToLower() == nickname.ToLower());

            if (CheckPlayerForExist(client))
            {
                client.Disconnect("User banned");
            }
        }

        private static bool CheckPlayerForExist(Client client)
        {
            if (client == null)
            {
                Console.WriteLine("Player not found");
                return false;
            }

            return true;
        }
    }
}
