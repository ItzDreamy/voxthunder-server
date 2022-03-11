using System.Collections.Generic;
using System.Numerics;

namespace VoxelTanksServer
{
    public class Map
    {
        public string? Name;

        public List<SpawnPoint> FirstTeamSpawns;

        public List<SpawnPoint> SecondTeamSpawns;

        public Map(string? name, List<SpawnPoint> firstTeamSpawns, List<SpawnPoint> secondTeamSpawns)
        {
            Name = name;
            FirstTeamSpawns = firstTeamSpawns;
            SecondTeamSpawns = secondTeamSpawns;
        }
    }
}