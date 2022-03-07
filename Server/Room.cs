using System.Collections.Generic;

namespace VoxelTanksServer
{
    public class Room
    {
        public bool IsOpen = true;
        public bool IsCached = false;
        public int MaxPlayers;
        public int PlayersPerTeam;
        
        public int PlayersCount => Players.Count;

        public Dictionary<int, Client?> Players = new Dictionary<int, Client?>();

        public List<CachedPlayer?> CachedPlayers = new List<CachedPlayer?>();
        //public List<Team> Teams = new List<Team>();

        public Room(int maxPlayers)
        {
            MaxPlayers = maxPlayers;
            PlayersPerTeam = maxPlayers / 2;
            Server.Rooms.Add(this);
            //Teams.Add(new Team());
            //Teams.Add(new Team());
        }
    }
}