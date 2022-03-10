using System.Numerics;

namespace VoxelTanksServer
{
    public class CachedPlayer
    {
        public string? Username;
        public string? TankName = "";
        public Team Team;
        public Vector3 Position;
        public Quaternion Rotation;
        public Quaternion BarrelRotation;
        public Quaternion TurretRotation;
        public int Health;
        public int TotalDamage;
        public int Kills;
        public bool CanShoot;
        public bool IsAlive;

        public CachedPlayer(Player player)
        {
            Username = player.Username;
            TankName = player.TankName;
            Team = player.Team;
            Position = player.Position;
            Rotation = player.Rotation;
            BarrelRotation = player.BarrelRotation;
            TurretRotation = player.TurretRotation;
            CanShoot = player.CanShoot;
            IsAlive = player.IsAlive;
            Health = player.Health;
            TotalDamage = player.TotalDamage;
            Kills = player.Kills;
        }
    }
}