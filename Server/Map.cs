using System.Collections.Generic;
using System.Numerics;

namespace VoxelTanksServer
{
    public class Map
    {
        public string? Name;

        public List<Vector3> FirstTeamSpawns;

        public List<Vector3> SecondTeamSpawns;

        public Map(string? name, List<Vector3> firstTeamSpawns, List<Vector3> secondTeamSpawns)
        {
            Name = name;
            FirstTeamSpawns = firstTeamSpawns;
            SecondTeamSpawns = secondTeamSpawns;
        }
    }
}