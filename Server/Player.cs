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
        
        private bool _canShoot;
        private bool _isAlive;
        

        public Player(int id, string username, Vector3 spawnPosition, string tankName, Room room)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = Quaternion.Identity;
            TankName = tankName;
            _isAlive = true;
            ConnectedRoom = room;
            
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
                if (speed < MaxSpeed && _isAlive)
                {
                    Position = nextPos;
                }
            }
            else
            {
                if (speed < MaxBackSpeed && _isAlive)
                {
                    Position = nextPos;
                }
            }

            if (_isAlive)
            {
                Rotation = rotation;
                BarrelRotation = barrelRotation;   
            }
            ServerSend.MovePlayer(ConnectedRoom, this);
        }

        public void RotateTurret(Quaternion turretRotation)
        {
            if(_isAlive)
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
            _isAlive = false;
            ServerSend.PlayerDead(Id);
        }

        public void Shoot(string bulletPrefab, string particlePrefab, Vector3 position, Quaternion rotation)
        {
            if (!_canShoot && !_isAlive)
                return;
            
            _canShoot = false;
            ServerSend.InstantiateObject(bulletPrefab, position, rotation, Id, ConnectedRoom);
            ServerSend.InstantiateObject(particlePrefab, position, rotation, Id, ConnectedRoom);
            
            Task.Run(async () =>
            {
                await Task.Delay((int) (Cooldown * 1000));
                _canShoot = true;
                return Task.CompletedTask;
            });
        }
    }
}