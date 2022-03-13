using System.Drawing;
using System.Numerics;

namespace VoxelTanksServer
{
    public static class ServerSend
    {
        private static void SendTCPData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Tcp.SendData(packet);
        }

        private static void SendTCPDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                Server.Clients[i].Tcp.SendData(packet);
            }
        }

        public static void SendTCPDataToRoom(Room? room, Packet packet)
        {
            packet.WriteLength();

            if (room != null)
            {
                foreach (var player in room.Players.Values)
                {
                    player.Tcp.SendData(packet);
                }
            }
        }

        public static void SendTCPDataToTeam(Team? team, Packet packet)
        {
            packet.WriteLength();
            
            foreach (var client in team.Players)
            {
                client.Tcp.SendData(packet);
            }
        }

        public static void SendTCPDataToRoom(Room? room, int exceptId, Packet packet)
        {
            packet.WriteLength();

            foreach (var player in room.Players.Values)
            {
                if (player.Id != exceptId)
                {
                    player.Tcp.SendData(packet);
                }
            }
        }

        public static void SendTCPDataToTeam(Team? team, int exceptId, Packet packet)
        {
            packet.WriteLength();

            foreach (var client in team.Players)
            {
                if (client.Id != exceptId)
                {
                    client.Tcp.SendData(packet);
                }
            }
        }

        private static void SendTCPDataToAll(int exceptClient, Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                if (i != exceptClient) Server.Clients[i].Tcp.SendData(packet);
            }
        }

        #region Packets

        public static void Welcome(int toClient, string? message)
        {
            using (Packet packet = new((int) ServerPackets.Welcome))
            {
                packet.Write(message);
                packet.Write(toClient);

                SendTCPData(toClient, packet);
            }
        }

        public static void SpawnPlayer(int toClient, Player? player)
        {
            using (Packet packet = new((int) ServerPackets.SpawnPlayer))
            {
                packet.Write(player.Id);
                packet.Write(player.Team.ID);
                packet.Write(player.Username);
                packet.Write(player.Position);
                packet.Write(player.Rotation);
                packet.Write(player.TurretRotation);
                packet.Write(player.BarrelRotation);
                packet.Write(player.TankName);
                packet.Write(player.Cooldown);
                packet.Write(!player.CanShoot);
                packet.Write(player.Health);
                packet.Write(player.MaxHealth);

                SendTCPData(toClient, packet);
            }
        }

        public static void MovePlayer(Room? room, Player player)
        {
            using (Packet packet = new((int) ServerPackets.PlayerMovement))
            {
                packet.Write(player.Id);
                packet.Write(player.Position);
                packet.Write(player.Rotation);
                packet.Write(player.BarrelRotation);

                SendTCPDataToRoom(room, packet);
            }
        }

        public static void RotateTurret(Player player)
        {
            using (Packet packet = new((int) ServerPackets.RotateTurret))
            {
                packet.Write(player.Id);
                packet.Write(player.TurretRotation);

                SendTCPDataToRoom(player.ConnectedRoom, player.Id, packet);
            }
        }

        public static void LoginResult(int toClient, bool result, string? message)
        {
            using (Packet packet = new((int) ServerPackets.LoginResult))
            {
                packet.Write(result);
                packet.Write(message);

                SendTCPData(toClient, packet);
            }
        }

        public static void InstantiateObject(string? name, Vector3 position, Quaternion rotation, int fromClient,
            Room? room)
        {
            using (Packet packet = new((int) ServerPackets.InstantiateObject))
            {
                packet.Write(name);
                packet.Write(position);
                packet.Write(rotation);
                packet.Write(fromClient);

                SendTCPDataToRoom(room, packet);
            }
        }

        public static void LoadScene(Room? room, string? sceneName)
        {
            using (Packet packet = new((int) ServerPackets.LoadGame))
            {
                packet.Write(sceneName);
                SendTCPDataToRoom(room, packet);
            }
        }

        public static void LoadScene(int toClient, string? sceneName)
        {
            using (Packet packet = new((int) ServerPackets.LoadGame))
            {
                packet.Write(sceneName);
                SendTCPData(toClient, packet);
            }
        }

        public static void PlayerDisconnected(int playerId, Room? room, bool isReconnected)
        {
            using (Packet packet = new((int) ServerPackets.PlayerDisconnected))
            {
                packet.Write(playerId);
                packet.Write(isReconnected);
                SendTCPDataToRoom(room, packet);
            }
        }

        public static void TakeDamage(int playerId, int maxHealth, int currentHealth)
        {
            using (Packet packet = new((int) ServerPackets.TakeDamage))
            {
                packet.Write(playerId);
                packet.Write(maxHealth);
                packet.Write(currentHealth);

                SendTCPData(playerId, packet);
            }
        }

        public static void PlayerDead(int playerId)
        {
            using (Packet packet = new((int) ServerPackets.PlayerDead))
            {
                packet.Write(playerId);

                SendTCPDataToRoom(Server.Clients[playerId].ConnectedRoom, packet);
            }
        }

        public static void AbleToReconnect(int playerId)
        {
            using (Packet packet = new((int) ServerPackets.AbleToReconnect))
            {
                SendTCPData(playerId, packet);
            }
        }

        public static void ShowDamage(int playerId, int damage, Vector3 position)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowDamage))
            {
                packet.Write(damage);
                packet.Write(position);
                SendTCPData(playerId, packet);
            }
        }
        
        public static void ShowKillFeed(Team team, Color color, string enemyUsername, string killedUsername)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowKillFeed))
            {
                packet.Write(enemyUsername);
                packet.Write(killedUsername);
                packet.Write(color);
                
                SendTCPDataToTeam(team, packet);
            }
        }
        
        #endregion
    }
}