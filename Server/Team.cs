using System.Collections.Generic;
using System.Numerics;

namespace VoxelTanksServer
{
    public class Team
    {
        public List<Client?> Players = new List<Client?>();
        public List<Vector3> SpawnPoints;
        public byte ID;
        
        public Team(byte id)
        {
            ID = id;
        }
    }
}