using System.Drawing;
using System.Numerics;
using VoxelTanksServer.DB;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;

namespace VoxelTanksServer.Protocol
{
    public static class ServerSend
    {
        private static void SendTcpData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Tcp.SendData(packet);
        }

        private static void SendTcpDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                Server.Clients[i].Tcp.SendData(packet);
            }
        }
        
        public static void SendTcpDataToRoom(Room? room, Packet packet)
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

        public static void SendTcpDataToTeam(Team? team, Packet packet)
        {
            packet.WriteLength();
            
            foreach (var client in team.Players)
            {
                client.Tcp.SendData(packet);
            }
        }

        public static void SendTcpDataToRoom(Room? room, int exceptId, Packet packet)
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

        public static void SendTcpDataToTeam(Team? team, int exceptId, Packet packet)
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

        private static void SendTcpDataToAll(int exceptClient, Packet packet)
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
                packet.Write(Server.Config.ClientVersion);

                SendTcpData(toClient, packet);
            }
        }

        public static void SpawnPlayer(int toClient, Player? player)
        {
            using (Packet packet = new((int) ServerPackets.SpawnPlayer))
            {
                packet.Write(player.ConnectedRoom.PlayersCount);
                packet.Write(player.Id);
                packet.Write(player.Team.Id);
                packet.Write(player.Username);
                packet.Write(player.Position);
                packet.Write(player.Rotation);
                packet.Write(player.TurretRotation);
                packet.Write(player.BarrelRotation);
                packet.Write(player.SelectedTank.Name);
                packet.Write(player.ConnectedRoom.PlayersLocked);
                
                SendTcpData(toClient, packet);
                
                InitializeTankStats(toClient, player);
            }
        }

        public static void InitializeTankStats(int toClient, Player? player)
        {
            using (Packet packet = new Packet((int) ServerPackets.InitializeTankStats))
            {
                packet.Write(player.Id);
                packet.Write(!player.CanShoot);
                packet.Write(player.SelectedTank.Cooldown);
                packet.Write(player.Health);
                packet.Write(player.SelectedTank.MaxHealth);
                packet.Write(player.SelectedTank.MaxSpeed);
                packet.Write(player.SelectedTank.MaxBackSpeed);
                packet.Write(player.SelectedTank.Acceleration);
                packet.Write(player.SelectedTank.BackAcceleration);
                packet.Write(player.SelectedTank.TankRotateSpeed);
                packet.Write(player.SelectedTank.TowerRotateSpeed);
                packet.Write(player.SelectedTank.AngleUp);
                packet.Write(player.SelectedTank.AngleDown);
                
                SendTcpData(toClient, packet);
            }
        }

        public static void SwitchTank(Client client, Tank tank, bool isOwned)
        {
            using (Packet packet = new Packet((int) ServerPackets.SwitchTank))
            {
                client.SelectedTank = tank;

                var topHealth = Server.Tanks.Max(t => t.MaxHealth);
                var topDamage = Server.Tanks.Max(t => t.Damage);
                var topSpeed = Server.Tanks.Max(t => t.MaxSpeed);
                
                packet.Write(isOwned);
                packet.Write(tank.Name);
                packet.Write(tank.MaxHealth);
                packet.Write(topHealth);
                packet.Write(tank.Damage);
                packet.Write(topDamage);
                packet.Write(tank.MaxSpeed);
                packet.Write(topSpeed);
                
                SendTcpData(client.Id, packet);
            }
        }
        
        public static void RotateTurret(Player player)
        {
            using (Packet packet = new((int) ServerPackets.RotateTurret))
            {
                packet.Write(player.Id);
                packet.Write(player.TurretRotation);
                packet.Write(player.BarrelRotation);

                SendTcpDataToRoom(player.ConnectedRoom, player.Id, packet);
            }
        }

        public static void LoginResult(int toClient, bool result, string? message)
        {
            using (Packet packet = new((int) ServerPackets.LoginResult))
            {
                packet.Write(result);
                packet.Write(message);

                SendTcpData(toClient, packet);
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

                SendTcpDataToRoom(room, packet);
            }
        }

        public static void LoadScene(Room? room, string? sceneName)
        {
            using (Packet packet = new((int) ServerPackets.LoadGame))
            {
                packet.Write(sceneName);
                SendTcpDataToRoom(room, packet);
            }
        }

        public static void LoadScene(int toClient, string? sceneName)
        {
            using (Packet packet = new((int) ServerPackets.LoadGame))
            {
                packet.Write(sceneName);
                SendTcpData(toClient, packet);
            }
        }

        public static void PlayerDisconnected(int playerId, Room? room)
        {
            using (Packet packet = new((int) ServerPackets.PlayerDisconnected))
            {
                packet.Write(playerId);
                SendTcpDataToRoom(room, packet);
            }
        }

        public static void PlayerReconnected(string username, Room? room)
        {
            using (Packet packet = new((int)ServerPackets.PlayerReconnected))
            {
                packet.Write(username);

                SendTcpDataToRoom(room, packet);
            }
        }

        public static void TakeDamage(int playerId, int maxHealth, int currentHealth)
        {
            using (Packet packet = new((int) ServerPackets.TakeDamage))
            {
                packet.Write(playerId);
                packet.Write(maxHealth);
                packet.Write(currentHealth);

                SendTcpData(playerId, packet);
            }
        }

        public static void PlayerDead(int playerId)
        {
            using (Packet packet = new((int) ServerPackets.PlayerDead))
            {
                packet.Write(playerId);

                SendTcpDataToRoom(Server.Clients[playerId].ConnectedRoom, packet);
            }
        }

        public static void AbleToReconnect(int toClient)
        {
            using (Packet packet = new((int) ServerPackets.AbleToReconnect))
            {
                SendTcpData(toClient, packet);
            }
        }

        public static void ShowDamage(int toClient, int damage, Player player)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowDamage))
            {
                packet.Write(player.Id);
                packet.Write(damage);
                
                SendTcpData(toClient, packet);
            }
        }

        public static void TakeDamageOtherPlayer(Room room, Player player)
        {
            using (Packet packet = new Packet((int) ServerPackets.TakeDamageOtherPlayer))
            {
                packet.Write(player.Id);
                packet.Write(player.SelectedTank.MaxHealth);
                packet.Write(player.Health);
                
                SendTcpDataToRoom(room, packet);
            }
        }
        
        public static void ShowKillFeed(Team team, Color color, string killerUsername, string deadUsername, string killerTank, string deadTank)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowKillFeed))
            {
                packet.Write(killerUsername);
                packet.Write(deadUsername);
                packet.Write(killerTank);
                packet.Write(deadTank);
                packet.Write(color);
                
                SendTcpDataToTeam(team, packet);
            }
        }
        
        public static void ShowPlayersCountInRoom(Room room)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowPlayersCountInRoom))
            {
                packet.Write(room.PlayersCount);
                packet.Write(room.MaxPlayers);
                SendTcpDataToRoom(room, packet);
            }
        }
        public static void SendPlayersStats(Room room)
        {
            using (Packet packet = new Packet((int)ServerPackets.PlayersStats))
            {
                packet.Write(room.Players.Values.ToList().Where(client => client.Player != null).Select(client => client.Player).ToList());
                SendTcpDataToRoom(room, packet);
            }
        }

        public static void EndGame(Team team, GameResults result)
        {
            using (Packet packet = new Packet((int) ServerPackets.EndGame))
            {
                packet.Write((int) result);
                SendTcpDataToTeam(team, packet);
            }
        }
        
        public static void LeaveToLobby(int toClient)
        {
            using (Packet packet = new Packet((int) ServerPackets.LeaveToLobby))
            {
                packet.Write("Lobby");
                SendTcpData(toClient, packet);
            }
        }
        
        public static void SendTimer(Room room, int time, bool isGeneral)
        {
            using (Packet packet = new Packet((int) ServerPackets.Timer))
            {
                packet.Write(time);
                packet.Write(isGeneral);
                SendTcpDataToRoom(room, packet);
            }
        }
        
        public static void UnlockPlayers(Room room)
        {
            using (Packet packet = new Packet((int) ServerPackets.UnlockPlayers))
            {
                SendTcpDataToRoom(room, packet);
            }
        }
        
        public static void SendMovementData(MovementData movement, Room room, int id)
        {
            using (Packet packet = new Packet((int) ServerPackets.SendMovement))
            {
                packet.Write(id);
                packet.Write(movement);
                
                SendTcpDataToRoom(room, packet);
            }
        }
        
        public static async void SendProfileData(Client toClient)
        {
            using (Packet packet = new Packet((int) ServerPackets.ProfileData))
            {
                PlayerStats stats = await DatabaseUtils.GetPlayerStats(toClient.Username);
                
                packet.Write(toClient.Username);
                packet.Write(stats.AvgDamage);
                packet.Write(stats.Battles);
                packet.Write(stats.WinRate);
                SendTcpData(toClient.Id, packet);
            }
        }
        
        public static void SendAuthId(string id, int toClient)
        {
            using (Packet packet = new Packet((int) ServerPackets.AuthId))
            {
                packet.Write(id);
                
                SendTcpData(toClient, packet);
            }
        }
        
        public static void SignOut(int toClient)
        {
            using (Packet packet = new Packet((int) ServerPackets.SignOut))
            {
                SendTcpData(toClient, packet);
            }
        }
        #endregion
    }
}