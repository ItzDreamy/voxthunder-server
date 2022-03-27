using System;
using System.Numerics;

namespace VoxelTanksServer
{
    /// <summary>
    /// Класс для хранения данных о спавнпоинте
    /// </summary>
    public class SpawnPoint : ICloneable
    {
        public bool IsOpen = true;
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

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

        /// <summary>
        /// Клонирование точки спавна
        /// </summary>
        /// <returns>Клон спавнпоинта</returns>
        public object Clone()
        {
            return new SpawnPoint(Position, Rotation);
        }
    }
}