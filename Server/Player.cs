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
        
        private bool _canShoot;
        

        public Player(int id, string username, Vector3 spawnPosition, string tankName)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = Quaternion.Identity;
            TankName = tankName;
            
            MaxHealth = (int) Database.RequestData("health", "tanksstats", "tankname", tankName.ToLower());
            Damage = (int) Database.RequestData("damage", "tanksstats", "tankname", tankName.ToLower());
            MaxSpeed = (float) Database.RequestData("maxSpeed", "tanksstats", "tankname", tankName.ToLower());
            MaxBackSpeed = (float) Database.RequestData("backSpeed", "tanksstats", "tankname", tankName.ToLower());
            Cooldown = (float) Database.RequestData("cooldown", "tanksstats", "tankname", tankName.ToLower());
            
            Health = MaxHealth;
            
            Task.Run(async () =>
            {
                await Task.Delay((int) (Cooldown * 1000));
                _canShoot = true;
                return Task.CompletedTask;
            });
        }
        
        
        public void Move(Vector3 nextPos, Quaternion rotation, Quaternion barrelRotation, float speed, bool isForward)
        {
            if (isForward)
            {
                if (speed < MaxSpeed)
                {
                    Position = nextPos;
                }
            }
            else
            {
                if (speed < MaxBackSpeed)
                {
                    Position = nextPos;
                }
            }
            Rotation = rotation;
            BarrelRotation = barrelRotation;
            ServerSend.MovePlayer(this);
        }

        public void RotateTurret(Quaternion turretRotation)
        {
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
        }

        public void Shoot(string bulletPrefab, string particlePrefab, Vector3 position, Quaternion rotation)
        {
            if (!_canShoot)
                return;
            
            _canShoot = false;
            ServerSend.InstantiateObject(bulletPrefab, position, rotation, Id);
            ServerSend.InstantiateObject(particlePrefab, position, rotation, Id);
            
            Task.Run(async () =>
            {
                await Task.Delay((int) (Cooldown * 1000));
                _canShoot = true;
                return Task.CompletedTask;
            });
        }
    }
}