using System.Numerics;
using System.Threading.Tasks;
using Serilog;

namespace VoxelTanksServer
{
    public class Player
    {
        public readonly int Id;
        public readonly string? Username;
        public string? TankName = "";
        public readonly Room? ConnectedRoom = null;
        public Team? Team;

        public Vector3 Position;
        public Quaternion Rotation;
        public Quaternion BarrelRotation;
        public Quaternion TurretRotation;
        public int Health;
        public int MaxHealth;
        public float MaxSpeed;
        public float MaxBackSpeed;
        public int Damage;
        public float Cooldown;
        public int TotalDamage;
        public int Kills;

        public bool CanShoot;
        public bool IsAlive;

        public Player(int id, string? username, Vector3 spawnPosition, Quaternion rotation, string? tankName,
            Room? room)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = rotation;
            TankName = tankName;
            IsAlive = true;
            ConnectedRoom = room;

            Tank tank = Server.Tanks.Find(tank => tank.Name == TankName.ToLower());
            MaxHealth = tank.MaxHealth;
            Damage = tank.Damage;
            MaxSpeed = tank.MaxSpeed;
            MaxBackSpeed = tank.MaxBackSpeed;
            Cooldown = tank.Cooldown;

            Health = MaxHealth;

            ConnectedRoom.CachedPlayers.Add(CachePlayer());

            Task.Run(async () =>
            {
                await Task.Delay((int) (Cooldown * 1000));
                CanShoot = true;
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Создает новый экземпляр игрока из кеша.
        /// </summary>
        /// <param name="cachedPlayer">Кэшированый игрок</param>
        /// <param name="id">ID игрока</param>
        public Player(CachedPlayer cachedPlayer, int id)
        {
            Id = id;
            Username = cachedPlayer.Username;
            TankName = cachedPlayer.TankName;
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

            Tank tank = Server.Tanks.Find(tank => tank.Name == cachedPlayer.TankName.ToLower());
            MaxHealth = tank.MaxHealth;
            Damage = tank.Damage;
            MaxSpeed = tank.MaxSpeed;
            MaxBackSpeed = tank.MaxBackSpeed;
            Cooldown = tank.Cooldown;

            ServerSend.PlayerDisconnected(Id, ConnectedRoom, true);
            ConnectedRoom.CachedPlayers.Remove(cachedPlayer);
            ConnectedRoom.CachedPlayers.Add(CachePlayer());

            if (!CanShoot && IsAlive)
            {
                Task.Run(async () =>
                {
                    await Task.Delay((int) (Cooldown * 1000));
                    CanShoot = true;
                    return Task.CompletedTask;
                });
            }
        }

        public void Move(Vector3 nextPos, Quaternion rotation, Quaternion barrelRotation, float speed, bool isForward)
        {
            //Anti-speedhack
            if (isForward)
            {
                if (speed < MaxSpeed && IsAlive)
                {
                    Position = nextPos;
                }
                else if (speed > MaxSpeed + 2)
                {
                    Server.Clients[Id].Disconnect();
                }
            }
            else
            {
                if (speed < MaxBackSpeed && IsAlive)
                {
                    Position = nextPos;
                }
                else if (speed > MaxBackSpeed + 2)
                {
                    Server.Clients[Id].Disconnect();
                }
            }

            if (IsAlive)
            {
                Rotation = rotation;
                BarrelRotation = barrelRotation;
            }

            //Cache player
            ConnectedRoom.CachedPlayers[
                    ConnectedRoom.CachedPlayers.IndexOf(
                        ConnectedRoom.CachedPlayers.Find(cachedPlayer => cachedPlayer?.Username == Username))] =
                CachePlayer();

            ServerSend.MovePlayer(ConnectedRoom, this);
        }

        public void RotateTurret(Quaternion turretRotation)
        {
            if (IsAlive)
                TurretRotation = turretRotation;
            ServerSend.RotateTurret(this);
        }

        public void TakeDamage(int damage, Player? enemy)
        {
            if (Health <= 0)
            {
                return;
            }


            Health -= damage;

            if (Health <= 0)
            {
                Health = 0;
                enemy.Kills++;
                Die();
            }

            ServerSend.TakeDamage(Id, MaxHealth, Health);
        }

        private void Die()
        {
            Log.Information($"Client {Id} with name {Username} died");
            IsAlive = false;
            ServerSend.PlayerDead(Id);
        }

        public void Shoot(string? bulletPrefab, string? particlePrefab, Vector3 position, Quaternion rotation)
        {
            if (!CanShoot && !IsAlive)
                return;

            CanShoot = false;
            ServerSend.InstantiateObject(bulletPrefab, position, rotation, Id, ConnectedRoom);
            ServerSend.InstantiateObject(particlePrefab, position, rotation, Id, ConnectedRoom);

            Task.Run(async () =>
            {
                await Task.Delay((int) (Cooldown * 1000));
                CanShoot = true;
                return Task.CompletedTask;
            });
        }

        public CachedPlayer? CachePlayer()
        {
            CachedPlayer? cachedPlayer =
                new CachedPlayer(this);
            return cachedPlayer;
        }
    }
}