using System;
using System.Data;
using System.Linq;
using System.Numerics;
using MySql.Data.MySqlClient;
using Serilog;

namespace VoxelTanksServer
{
    /// <summary>
    /// Обработчик пакетов сервера
    /// </summary>
    public static class PacketsHandler
    {
        /// <summary>
        /// Успешное подключения клиента к серверу
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void WelcomePacketReceived(int fromClient, Packet packet)
        {
            int clientIdCheck = packet.ReadInt();
            Log.Information(
                $"{Server.Clients[fromClient]?.Tcp?.Socket?.Client.RemoteEndPoint} connected successfully with ID {fromClient}");

            if (fromClient != clientIdCheck)
            {
                Log.Warning(
                    $"Player (ID: {fromClient}) has the wrong client ID ({clientIdCheck})");
            }
        }

        /// <summary>
        /// Сообщение о доступности спавна игроков
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void ReadyToSpawnReceived(int fromClient, Packet packet)
        {
            var player = Server.Clients[fromClient];

            if (!player.IsAuth)
            {
                player.Disconnect("Игрок не вошел в аккаунт");
            }

            if (player.Tcp.Socket == null) return;
            
            player.ReadyToSpawn = true;

            //Если все игроки готовы - спавн игроков
            if (CheckPlayersReady(player.ConnectedRoom))
            {
                foreach(Client client in player.ConnectedRoom.Players.Values)
                {
                    client.SendIntoGame(client.Username, client.SelectedTank);
                }
            }
        }

        /// <summary>
        /// Проверка игроков на готовность к спавну
        /// </summary>
        /// <param name="room"></param>
        /// <returns>Готовность к спавну</returns>
        private static bool CheckPlayersReady(Room room)
        {
            foreach (var client in room.Players.Values)
            {
                if (!client.ReadyToSpawn)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Смена танка
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void ChangeTank(int fromClient, Packet packet)
        {
            string? tankName = packet.ReadString();
            var client = Server.Clients[fromClient];

            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }

            //TODO: Check owned tanks
            
            var tank = Server.Tanks.Find(tank => string.Equals(tank.Name, tankName, StringComparison.CurrentCultureIgnoreCase));
            if (tank == null)
            {
                client.Disconnect("Неизвестный танк");
                return;
            }
            
            ServerSend.SwitchTank(client, tank);
        }

        /// <summary>
        /// Движение игрока
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void PlayerMovement(int fromClient, Packet packet)
        {
            if (!Server.Clients[fromClient].IsAuth)
            {
                Server.Clients[fromClient].Disconnect("Игрок не вошел в аккаунт");
            }

            //Чтение данных о позиции, повороте, скорости, направлении
            Vector3 playerVelocity = packet.ReadVector3();
            Quaternion playerRotation = packet.ReadQuaternion();
            Quaternion barrelRotation = packet.ReadQuaternion();
            bool isForward = packet.ReadBool();

            Player? player = Server.Clients[fromClient].Player;
            //Движение
            player?.Move(playerVelocity, playerRotation, barrelRotation,  (float) Math.Sqrt((double) playerVelocity.X * (double) playerVelocity.X + (double) playerVelocity.Y * (double) playerVelocity.Y + (double) playerVelocity.Z * (double) playerVelocity.Z), isForward);
        }
        
        public static void SetPlayerPosition(int fromClient, Packet packet)
        {
            if (!Server.Clients[fromClient].IsAuth)
            {
                Server.Clients[fromClient].Disconnect("Игрок не вошел в аккаунт");
                return;
            }

            Vector3 position = packet.ReadVector3();
            
            Player? player = Server.Clients[fromClient].Player;
            if (player == null)
            {
                return;
            }
            player.Position = position + player.Velocity * ((float) (DateTime.Now - player.PreviousMoveTime).Milliseconds / 1000);
            
            ServerSend.SendPlayerPosition(player.ConnectedRoom, player);
        }

        /// <summary>
        /// Поворот башни
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void RotateTurret(int fromClient, Packet packet)
        {
            var client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }

            //Чтение поворота башни
            Quaternion turretRotation = packet.ReadQuaternion();
            Player? player = Server.Clients[fromClient].Player;
            if (player != null)
            {
                //Поворот башни
                player.RotateTurret(turretRotation);
            }
        }

        /// <summary>
        /// Попытка входа в игру
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static async void TryLogin(int fromClient, Packet packet)
        {
            //Чтение ника и пароля
            string? username = packet.ReadString();
            string? password = packet.ReadString();

            //Проверка на корректность логина и пароля
            await AuthorizationHandler.TryLogin(username, password, Server.Clients[fromClient].Tcp.Socket.Client.RemoteEndPoint?.ToString(), fromClient);
        }

        /// <summary>
        /// Спавн какого-либо объекта
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void InstantiateObject(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }
            
            //Чтение имени и позиции префаба
            string? name = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            //Спавн объекта для всех игроков комнаты
            ServerSend.InstantiateObject(name, position, rotation, fromClient,
                Server.Clients[fromClient].ConnectedRoom);
        }

        /// <summary>
        /// Выстрел
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void ShootBullet(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }

            //Чтение данных префаба
            string? name = packet.ReadString();
            string? particlePrefab = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            Player? player = Server.Clients[fromClient].Player;
            //Выстрел
            player?.Shoot(name, particlePrefab, position, rotation);
        }

        /// <summary>
        /// Получение урона
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void TakeDamage(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }

            //Чтение id врага
            int enemyId = packet.ReadInt();
            Player? enemy = Server.Clients[enemyId].Player;
            Player? hitPlayer = Server.Clients[fromClient].Player;
            
            if (enemy != null && hitPlayer != null && enemy.Team.ID != hitPlayer.Team.ID)
            {
                //Высчитывание урона
                int damage = enemy.SelectedTank.Damage;
                float randomCoof = new Random().Next(-20, 20) * ((float) damage / 100);
                int calculatedDamage = damage + (int) randomCoof;

                if (calculatedDamage == 0)
                {
                    calculatedDamage = damage;
                }
                else if (calculatedDamage > hitPlayer.Health)
                {
                    calculatedDamage = hitPlayer.Health;
                }

                //Нанесение урона
                hitPlayer.TakeDamage(calculatedDamage, enemy);
                //Подсчет суммарного урона врага
                enemy.TotalDamage += calculatedDamage;
                
                if (hitPlayer.Health > 0)
                {
                    //Показ урона
                    ServerSend.ShowDamage(enemy.Id, calculatedDamage, hitPlayer.Position);
                }
            }
        }

        /// <summary>
        /// Подключится или создать комнату, если нет доступной
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void JoinOrCreateRoom(int fromClient, Packet packet)
        {
            Client packetSender = Server.Clients[fromClient];
            if (!packetSender.IsAuth)
            {
                packetSender.Disconnect("Игрок не вошел в аккаунт");
            }

            //Поиск доступной комнаты, если существует - подключение
            if (Server.Rooms.Count > 0)
            {
                foreach (var room in Server.Rooms)
                {
                    if (room.IsOpen)
                    {
                        Client client = Server.Clients[fromClient];
                        if (client == null) return;
                        //Присоединение к комнате
                        client.JoinRoom(room);

                        //Закрытие комнаты и балансировка команд, если комната заполнена
                        if (room.PlayersCount == room.MaxPlayers)
                        {
                            room.IsOpen = false;
                            room.BalanceTeams();
                        }

                        return;
                    }
                }
            }

            //Создание новой комнаты
            Room? newRoom = new Room(2);
            //Присоединение к комнате
            Server.Clients[fromClient].JoinRoom(newRoom);

            //Закрытие комнаты и балансировка команд, если комната заполнена
            if (newRoom.PlayersCount == newRoom.MaxPlayers)
            {
                newRoom.IsOpen = false;
                newRoom.BalanceTeams();
            }
        }

        /// <summary>
        /// Покинуть комнату
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void LeaveRoom(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }
            
            Room? playerRoom = Server.Clients[fromClient].ConnectedRoom;
            if (playerRoom is {IsOpen: true})
            {
                Server.Clients[fromClient].LeaveRoom();
            }
        }

        /// <summary>
        /// Проверка на доступность переподключения к игре
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void CheckAbleToReconnect(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }
            
            //Поиск игрока в кеше комнат
            foreach (var room in Server.Rooms)
            {
                foreach (var cachedPlayer in room.CachedPlayers)
                {
                    //Если игрок найден - оповещение игрока
                    if (cachedPlayer?.Username.ToLower() == Server.Clients[fromClient].Username?.ToLower() && cachedPlayer.IsAlive)
                    {
                        ServerSend.AbleToReconnect(fromClient);
                        Log.Information($"{Server.Clients[fromClient].Username} can reconnect to battle");
                    }
                }
            }
        }
        
        /// <summary>
        /// Переподключение к игре
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void Reconnect(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }

            foreach (var room in Server.Rooms)
            {
                foreach (var cachedPlayer in room?.CachedPlayers!)
                {
                    if (cachedPlayer?.Username?.ToLower() == Server.Clients[fromClient].Username?.ToLower())
                    {
                        //Подключение к комнате
                        client.JoinRoom(room);
                        //Присоединение к команде
                        client.Team = cachedPlayer?.Team;
                        client?.Team?.Players.Add(client);
                        //Загрузка игры
                        ServerSend.LoadScene(fromClient, room.Map.Name);
                        //Создание нового игрока из кеша
                        client.Player = new Player(cachedPlayer, fromClient);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Отмена переподключения к игре
        /// </summary>
        /// <param name="fromClient"></param>
        /// <param name="packet"></param>
        public static void CancelReconnect(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect("Игрок не вошел в аккаунт");
            }

            foreach (var room in Server.Rooms)
            {
                foreach (var cachedPlayer in room.CachedPlayers)
                {
                    if (cachedPlayer?.Username == Server.Clients[fromClient].Username)
                    {
                        //Удаление кеша игрока из комнаты
                        Log.Information($"{cachedPlayer?.Username} canceled reconnect");
                        room.CachedPlayers[room.CachedPlayers.IndexOf(cachedPlayer)] = null;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Собирает данные о боевой статистике игроков и отсылает клиенту
        /// </summary>
        public static void RequestPlayersStats(int fromClient, Packet packet)
        {
            Room room = Server.Clients[fromClient].ConnectedRoom;

            if (room == null || room.Players == null)
            {
                return;
            }

            ServerSend.SendPlayersStats(room);
        }

        
    }
}