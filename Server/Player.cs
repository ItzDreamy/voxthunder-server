using System.Data;
using System.Numerics;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Serilog;

namespace VoxelTanksServer
{
    public class Player
    {
        public int Id;
        public string Username;
        public string TankName = "";
        public Room ConnectedRoom = null;

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
        public int TotalDamage = 0;
        public int Kills = 0;

        public bool CanShoot;
        public bool IsAlive;

        public Player(int id, string username, Vector3 spawnPosition, string tankName, Room room)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = Quaternion.Identity;
            TankName = tankName;
            IsAlive = true;
            ConnectedRoom = room;

            MaxHealth = (int) Database.RequestData("health", "tanksstats", "tankname", tankName.ToLower());
            Damage = (int) Database.RequestData("damage", "tanksstats", "tankname", tankName.ToLower());
            MaxSpeed = (float) Database.RequestData("maxSpeed", "tanksstats", "tankname", tankName.ToLower());
            MaxBackSpeed = (float) Database.RequestData("backSpeed", "tanksstats", "tankname", tankName.ToLower());
            Cooldown = (float) Database.RequestData("cooldown", "tanksstats", "tankname", tankName.ToLower());

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

            Position = cachedPlayer.Position;
            Rotation = cachedPlayer.Rotation;
            BarrelRotation = cachedPlayer.BarrelRotation;
            TurretRotation = cachedPlayer.TurretRotation;
            CanShoot = cachedPlayer.CanShoot;
            IsAlive = cachedPlayer.IsAlive;
            Health = cachedPlayer.Health;
            TotalDamage = cachedPlayer.TotalDamage;
            ConnectedRoom = Server.Clients[Id].ConnectedRoom;
            
            MaxHealth = (int) Database.RequestData("health", "tanksstats", "tankname", TankName.ToLower());
            Damage = (int) Database.RequestData("damage", "tanksstats", "tankname", TankName.ToLower());
            MaxSpeed = (float) Database.RequestData("maxSpeed", "tanksstats", "tankname", TankName.ToLower());
            MaxBackSpeed = (float) Database.RequestData("backSpeed", "tanksstats", "tankname", TankName.ToLower());
            Cooldown = (float) Database.RequestData("cooldown", "tanksstats", "tankname", TankName.ToLower());
            
            ServerSend.PlayerDisconnected(Id, true);
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
            if (isForward)
            {
                if (speed < MaxSpeed && IsAlive)
                {
                    Position = nextPos;
                }
            }
            else
            {
                if (speed < MaxBackSpeed && IsAlive)
                {
                    Position = nextPos;
                }
            }

            if (IsAlive)
            {
                Rotation = rotation;
                BarrelRotation = barrelRotation;
            }

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

        public void TakeDamage(int damage)
        {
            if (Health <= 0)
            {
                return;
            }

            Health -= damage;

            if (Health <= 0)
            {
                Health = 0;
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

        public void Shoot(string bulletPrefab, string particlePrefab, Vector3 position, Quaternion rotation)
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
                new CachedPlayer(Username, TankName, Position, Rotation, BarrelRotation, TurretRotation, Health,
                    TotalDamage, CanShoot, IsAlive);
            return cachedPlayer;
        }
    }
}