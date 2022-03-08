using System.Numerics;

namespace VoxelTanksServer
{
    public class CachedPlayer
    {
        public string Username;
        public string TankName = "";

        public Vector3 Position;
        public Quaternion Rotation;
        public Quaternion BarrelRotation;
        public Quaternion TurretRotation;
        public int Health;
        public int TotalDamage;
        public int Kills;
        public bool CanShoot;
        public bool IsAlive;

        public CachedPlayer(string username, string tankName, Vector3 position, Quaternion rotation, Quaternion barrelRotation, Quaternion turretRotation, int health, int totalDamage, bool canShoot, bool isAlive)
        {
            Username = username;
            TankName = tankName;
            Position = position;
            Rotation = rotation;
            BarrelRotation = barrelRotation;
            TurretRotation = turretRotation;
            CanShoot = canShoot;
            IsAlive = isAlive;
            Health = health;
            TotalDamage = totalDamage;
        }
    }
}