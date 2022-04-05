using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace VoxelTanksServer
{
    /// <summary>
    /// Класс для отправки данных клиенту
    /// </summary>
    public static class ServerSend
    {
        /// <summary>
        /// Метод для отправки данных определенному клиенту
        /// </summary>
        /// <param name="toClient"></param>
        /// <param name="packet"></param>
        private static void SendTCPData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Tcp.SendData(packet);
        }

        /// <summary>
        /// Отправка данных всем клиентам на сервере
        /// </summary>
        /// <param name="packet"></param>
        private static void SendTCPDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                Server.Clients[i].Tcp.SendData(packet);
            }
        }
        /// <summary>
        /// Отправка данных всем клиентам в указаной комнате
        /// </summary>
        /// <param name="room"></param>
        /// <param name="packet"></param>
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

        /// <summary>
        /// Отправка данных всем клиентам в указаной команде
        /// </summary>
        /// <param name="team"></param>
        /// <param name="packet"></param>
        public static void SendTCPDataToTeam(Team? team, Packet packet)
        {
            packet.WriteLength();
            
            foreach (var client in team.Players)
            {
                client.Tcp.SendData(packet);
            }
        }

        /// <summary>
        /// Отправка данных всем клиентам в указаной комнате кроме одного
        /// </summary>
        /// <param name="room"></param>
        /// <param name="exceptId"></param>
        /// <param name="packet"></param>
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

        /// <summary>
        /// Отправка данных всем клиентам в указаной команде кроме одного
        /// </summary>
        /// <param name="team"></param>
        /// <param name="exceptId"></param>
        /// <param name="packet"></param>
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

        /// <summary>
        /// Отправка данных всем игрокам на сервере кроме одного
        /// </summary>
        /// <param name="exceptClient"></param>
        /// <param name="packet"></param>
        private static void SendTCPDataToAll(int exceptClient, Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                if (i != exceptClient) Server.Clients[i].Tcp.SendData(packet);
            }
        }

        #region Packets

        /// <summary>
        /// Приветственный пакет
        /// </summary>
        /// <param name="toClient"></param>
        /// <param name="message"></param>
        public static void Welcome(int toClient, string? message)
        {
            using (Packet packet = new((int) ServerPackets.Welcome))
            {
                packet.Write(message);
                packet.Write(toClient);

                SendTCPData(toClient, packet);
            }
        }

        /// <summary>
        /// Спавн игрока
        /// </summary>
        /// <param name="toClient"></param>
        /// <param name="player"></param>
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
                packet.Write(player.SelectedTank.Name);
                
                SendTCPData(toClient, packet);
                
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
                
                SendTCPData(toClient, packet);
            }
        }

        public static void SwitchTank(Client client, Tank tank)
        {
            using (Packet packet = new Packet((int) ServerPackets.SwitchTank))
            {
                client.SelectedTank = tank;

                var topHealth = Server.Tanks.Max(t => t.MaxHealth);
                var topDamage = Server.Tanks.Max(t => t.Damage);
                var topSpeed = Server.Tanks.Max(t => t.MaxSpeed);
                
                packet.Write(tank.Name);
                packet.Write(tank.MaxHealth);
                packet.Write(topHealth);
                packet.Write(tank.Damage);
                packet.Write(topDamage);
                packet.Write(tank.MaxSpeed);
                packet.Write(topSpeed);
                
                SendTCPData(client.Id, packet);
            }
        }
        
        /// <summary>
        /// Движение игрока
        /// </summary>
        /// <param name="room"></param>
        /// <param name="player"></param>
        public static void MovePlayer(Room? room, Player player)
        {
            using (Packet packet = new((int) ServerPackets.PlayerMovement))
            {
                packet.Write(player.Id);
                packet.Write(player.Velocity);
                packet.Write(player.Rotation);
                packet.Write(player.BarrelRotation);
                packet.Write(DateTime.Now);

                SendTCPDataToRoom(room, packet);
            }
        }

        public static void SendPlayerPosition(Room? room, Player player)
        {
            using (Packet packet = new Packet((int) ServerPackets.PlayerPosition))
            {
                packet.Write(player.Id);
                packet.Write(player.Position);
                packet.Write(player.Rotation);
                //packet.Write(DateTime.Now);
                
                SendTCPDataToRoom(room, packet);
            }
        }
        
        /// <summary>
        /// Поворот башни
        /// </summary>
        /// <param name="player"></param>
        public static void RotateTurret(Player player)
        {
            using (Packet packet = new((int) ServerPackets.RotateTurret))
            {
                packet.Write(player.Id);
                packet.Write(player.TurretRotation);
                packet.Write(player.BarrelRotation);

                SendTCPDataToRoom(player.ConnectedRoom, player.Id, packet);
            }
        }

        /// <summary>
        /// Отправка результата авторизации
        /// </summary>
        /// <param name="toClient"></param>
        /// <param name="result"></param>
        /// <param name="message"></param>
        public static void LoginResult(int toClient, bool result, string? message)
        {
            using (Packet packet = new((int) ServerPackets.LoginResult))
            {
                packet.Write(result);
                packet.Write(message);

                SendTCPData(toClient, packet);
            }
        }

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="fromClient"></param>
        /// <param name="room"></param>
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

        /// <summary>
        /// Загрузка сцены для комнаты
        /// </summary>
        /// <param name="room"></param>
        /// <param name="sceneName"></param>
        public static void LoadScene(Room? room, string? sceneName)
        {
            using (Packet packet = new((int) ServerPackets.LoadGame))
            {
                packet.Write(sceneName);
                SendTCPDataToRoom(room, packet);
            }
        }

        /// <summary>
        /// Загрузка сцены для игрока
        /// </summary>
        /// <param name="toClient"></param>
        /// <param name="sceneName"></param>
        public static void LoadScene(int toClient, string? sceneName)
        {
            using (Packet packet = new((int) ServerPackets.LoadGame))
            {
                packet.Write(sceneName);
                SendTCPData(toClient, packet);
            }
        }

        /// <summary>
        /// Отправка пакета когда игрок отключился
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="room"></param>
        public static void PlayerDisconnected(int playerId, Room? room)
        {
            using (Packet packet = new((int) ServerPackets.PlayerDisconnected))
            {
                packet.Write(playerId);
                SendTCPDataToRoom(room, packet);
            }
        }

        /// <summary>
        /// Отправка пакета когда игрок переподключился
        /// </summary>
        /// <param name="username"></param>
        /// <param name="room"></param>
        public static void PlayerReconnected(string username, Room? room)
        {
            using (Packet packet = new((int)ServerPackets.PlayerReconnected))
            {
                packet.Write(username);

                SendTCPDataToRoom(room, packet);
            }
        }

        /// <summary>
        /// Получение урона
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="maxHealth"></param>
        /// <param name="currentHealth"></param>
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

        /// <summary>
        /// Уничтожение игрока
        /// </summary>
        /// <param name="playerId"></param>
        public static void PlayerDead(int playerId)
        {
            using (Packet packet = new((int) ServerPackets.PlayerDead))
            {
                packet.Write(playerId);

                SendTCPDataToRoom(Server.Clients[playerId].ConnectedRoom, packet);
            }
        }

        /// <summary>
        /// Отправить уведомление о том, что игрок может переподключится к игре
        /// </summary>
        /// <param name="playerId"></param>
        public static void AbleToReconnect(int playerId)
        {
            using (Packet packet = new((int) ServerPackets.AbleToReconnect))
            {
                SendTCPData(playerId, packet);
            }
        }

        /// <summary>
        /// Показать наносимый урон
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="damage"></param>
        /// <param name="position"></param>
        public static void ShowDamage(int playerId, int damage, Vector3 position)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowDamage))
            {
                packet.Write(damage);
                packet.Write(position);
                SendTCPData(playerId, packet);
            }
        }
        
        /// <summary>
        /// Показать килфид
        /// </summary>
        /// <param name="team"></param>
        /// <param name="color"></param>
        /// <param name="killerUsername"></param>
        /// <param name="deadUsername"></param>
        /// <param name="killerTank"></param>
        /// <param name="deadTank"></param>
        public static void ShowKillFeed(Team team, Color color, string killerUsername, string deadUsername, string killerTank, string deadTank)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowKillFeed))
            {
                packet.Write(killerUsername);
                packet.Write(deadUsername);
                packet.Write(killerTank);
                packet.Write(deadTank);
                packet.Write(color);
                
                SendTCPDataToTeam(team, packet);
            }
        }
        
        /// <summary>
        /// Показать кол-во игроков в комнате
        /// </summary>
        /// <param name="room"></param>
        public static void ShowPlayersCountInRoom(Room room)
        {
            using (Packet packet = new Packet((int) ServerPackets.ShowPlayersCountInRoom))
            {
                packet.Write(room.PlayersCount);
                packet.Write(room.MaxPlayers);
                SendTCPDataToRoom(room, packet);
            }
        }

        /// <summary>
        /// Отправить статистику всех игроков в комнате клиенту
        /// </summary>
        /// <param name="room">Комната в которой находятся игроки</param>
        public static void SendPlayersStats(Room room)
        {
            using (Packet packet = new Packet((int)ServerPackets.PlayersStats))
            {
                packet.Write(room.Players.Values.ToList().Where(client => client.Player != null).Select(client => client.Player).ToList());
                SendTCPDataToRoom(room, packet);
            }
        }

        public static void EndGame(Room room)
        {
            using (Packet packet = new Packet((int) ServerPackets.EndGame))
            {
                SendTCPDataToRoom(room, packet);
            }
        }
        
        public static void SendInput(Room room, int playerId, bool accelerate, bool brake, float turn)
        {
            using (Packet packet = new Packet((int) ServerPackets.PlayerInput))
            {
                packet.Write(playerId);
                packet.Write(accelerate);
                packet.Write(brake);
                packet.Write(turn);
                
                SendTCPDataToRoom(room, packet);
            }
        }
        #endregion
    }
}