using System;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;

namespace VoxelTanksServer
{
    public class Player
    {
        public Team? Team;
        public int TotalDamage;

        public int Id { get; private set; }
        public string? Username { get; private set; }
        public Tank SelectedTank { get; private set; }
        public Room? ConnectedRoom { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; private set; }
        public Quaternion Rotation { get; set; }
        public Quaternion BarrelRotation { get; private set; }
        public Quaternion TurretRotation { get; private set; }
        public int Health { get; private set; }
        public int Kills { get; private set; }
        public bool CanShoot { get; private set; }
        public bool IsAlive { get; private set; }

        public DateTime PreviousMoveTime;

        public Player(int id, string? username, Vector3 spawnPosition, Quaternion rotation, Tank tank,
            Room? room)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = rotation;
            SelectedTank = tank;
            IsAlive = true;
            ConnectedRoom = room;

            if (tank == null)
            {
                Server.Clients[Id].Disconnect("Неизвестный танк");
                return;
            }

            Health = tank.MaxHealth;

            ConnectedRoom.CachedPlayers.Add(CachePlayer());

            Task.Run(async () =>
            {
                await Task.Delay((int) (SelectedTank.Cooldown * 1000));
                CanShoot = true;
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Создает новый экземпляр игрока из кеша
        /// </summary>
        /// <param name="cachedPlayer">Кэшированый игрок</param>
        /// <param name="id">ID игрока</param>
        public Player(CachedPlayer cachedPlayer, int id)
        {
            Id = id;
            Username = cachedPlayer.Username;
            Team = cachedPlayer.Team;

            Position = cachedPlayer.Position;
            Rotation = cachedPlayer.Rotation;
            BarrelRotation = cachedPlayer.BarrelRotation;
            TurretRotation = cachedPlayer.TurretRotation;
            CanShoot = cachedPlayer.CanShoot;
            IsAlive = cachedPlayer.IsAlive;
            Health = cachedPlayer.Health;
            TotalDamage = cachedPlayer.TotalDamage;
            Kills = cachedPlayer.Kills;
            ConnectedRoom = Server.Clients[Id].ConnectedRoom;

            SelectedTank = cachedPlayer.SelectedTank;

            ServerSend.PlayerReconnected(Username, ConnectedRoom);

            ConnectedRoom.CachedPlayers.Remove(cachedPlayer);
            ConnectedRoom.CachedPlayers.Add(CachePlayer());

            if (!CanShoot && IsAlive)
            {
                Task.Run(async () =>
                {
                    await Task.Delay((int) (SelectedTank.Cooldown * 1000));
                    CanShoot = true;
                    return Task.CompletedTask;
                });
            }
        }


        /// <summary>
        /// Движение игрока + проверка на спидхак
        /// </summary>
        /// <param name="nextPos">Следующая позиция клиента</param>
        /// <param name="rotation">Следующий поворот клиента</param>
        /// <param name="barrelRotation">Следующий поворот дула клиента</param>
        /// <param name="speed">Скорость клиента</param>
        /// <param name="isForward">Направление движения</param>
        public void Move(Vector3 velocity, Quaternion rotation, Quaternion barrelRotation, float speed, bool isForward)
        {
            if (!IsAlive || CheckSpeedHack(speed,
                isForward ? SelectedTank.MaxSpeed : SelectedTank.MaxBackSpeed)) return;

            Rotation = rotation;
            Velocity = velocity;
            BarrelRotation = barrelRotation;

            //Отправка данных о позиции и повороте игрока всем игрокам комнаты
            ServerSend.MovePlayer(ConnectedRoom, this);
        }

        private bool CheckSpeedHack(float speed, float maxSpeed)
        {
            if (speed < maxSpeed) return false;
            //Server.Clients[Id].Disconnect("Подозрение в спидкахе");
            return false;
        }

        /// <summary>
        /// Поворот башни
        /// </summary>
        /// <param name="turretRotation"></param>
        public void RotateTurret(Quaternion turretRotation, Quaternion barrelRotation)
        {
            if (IsAlive)
            {
                TurretRotation = turretRotation;
                BarrelRotation = barrelRotation;
            }
            //Отправка данных о повороте башни игрока всем игрокам комнаты
            ServerSend.RotateTurret(this);
        }

        /// <summary>
        /// Получение урона
        /// </summary>
        /// <param name="damage">Урон</param>
        /// <param name="enemy">Враг</param>
        public void TakeDamage(int damage, Player enemy)
        {
            if (Health <= 0)
            {
                return;
            }

            Health -= damage;

            if (Health <= 0)
            {
                Health = 0;
                Die(enemy);
            }

            ServerSend.TakeDamage(Id, SelectedTank.MaxHealth, Health);
        }

        /// <summary>
        /// Смерть игрока
        /// </summary>
        /// <param name="enemy">Враг</param>
        private void Die(Player enemy)
        {
            enemy.Kills++;

            IsAlive = false;

            ServerSend.PlayerDead(Id);

            foreach (var team in ConnectedRoom.Teams)
            {
                ServerSend.ShowKillFeed(team, team == Team ? Color.Red : Color.Lime, enemy.Username, Username, enemy.SelectedTank.Name,
                    SelectedTank.Name);
            }
            
            //Если все игроки команды мертвы - заканчивать игру
            if (!Team.PlayersAliveCheck())
            {
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    ServerSend.SendPlayersStats(ConnectedRoom);
                
                    foreach (var team in ConnectedRoom.Teams)
                    {
                        ServerSend.EndGame(team, team != Team, false);
                    }
                });
            }
        }

        /// <summary>
        /// Выстрел
        /// </summary>
        /// <param name="bulletPrefab">Название префаба пули</param>
        /// <param name="particlePrefab">Название префаба партиклов выстрела</param>
        /// <param name="position">Позиция пули</param>
        /// <param name="rotation">Поворот пули</param>
        public void Shoot(string? bulletPrefab, string? particlePrefab, Vector3 position, Quaternion rotation)
        {
            //Проверка на возможность выстрела
            if (!CanShoot || !IsAlive)
                return;

            CanShoot = false;
            //Создание пули и эффектов
            ServerSend.InstantiateObject(bulletPrefab, position, rotation, Id, ConnectedRoom);
            ServerSend.InstantiateObject(particlePrefab, position, rotation, Id, ConnectedRoom);

            //Таймер до следующего выстрела
            Task.Run(async () =>
            {
                await Task.Delay((int) (SelectedTank.Cooldown * 1000));
                CanShoot = true;
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Кеширование игрока память комнаты
        /// </summary>
        /// <returns></returns>
        public CachedPlayer? CachePlayer()
        {
            return new CachedPlayer(this);
        }
    }
}