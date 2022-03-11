using System.Numerics;

namespace VoxelTanksServer
{
    public class SpawnPoint
    {
        public bool IsOpen = true;
        public Vector3 Position;
        public Quaternion Rotation;

        public SpawnPoint(Vector3 position)
        {
            Rotation = Quaternion.Identity;
            Position = position;
        }   
        
        public SpawnPoint(Vector3 position, Quaternion rotation)
        {
            Rotation = rotation;
            Position = position;
        }   
    }
}