using System.Numerics;

namespace VoxelTanksServer
{
    public class Player
    {
        public int Id;
        public string Username;

        public Vector3 Position;
        public Quaternion Rotation;
        public Quaternion BarrelRotation;
        public Quaternion TurretRotation;
        
        public string TankName = "";

        public Player(int id, string username, Vector3 spawnPosition, string tankName)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = Quaternion.Identity;
            TankName = tankName;
        }

        public void Move(Vector3 position, Quaternion rotation, Quaternion barrelRotation)
        {
            Position = position;
            Rotation = rotation;
            BarrelRotation = barrelRotation;
            ServerSend.MovePlayer(this);
        }

        public void RotateTurret(Quaternion turretRotation)
        {
            TurretRotation = turretRotation;
            ServerSend.RotateTurret(this);
        }
    }
}