﻿using System.Collections.Generic;

namespace VoxelTanksServer
{
    public class Team
    {
        public List<Client?> Players = new();
        public List<SpawnPoint> SpawnPoints;
        public byte ID;
        
        public Team(byte id, List<SpawnPoint> spawnPoints)
        {
            ID = id;
            SpawnPoints = spawnPoints;
        }
    }
}