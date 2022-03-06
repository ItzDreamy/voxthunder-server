using System.Collections.Generic;

namespace VoxelTanksServer
{
    public class Room
    {
        public bool IsOpen = true;
        public int MaxPlayers;
        public int PlayersPerTeam;
        
        public int PlayersCount => Players.Count;

        public Dictionary<int, Player?> Players = new Dictionary<int, Player?>();
        public List<Team> Teams = new List<Team>();

        public Room(int maxPlayers)
        {
            MaxPlayers = maxPlayers;
            PlayersPerTeam = maxPlayers / 2;
            Teams.Add(new Team());
            Teams.Add(new Team());
        }
        
        public Player? GetPlayer(int playerId)
        {
            Players.TryGetValue(playerId, out Player? player);

            return player;
        }
        
        public void LeftRoom(Player player)
        {
            Players.Remove(player.Id);
            player.ConnectedRoom = null;
        }
    }
}