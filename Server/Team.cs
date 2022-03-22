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

        public bool PlayersDeathCheck() 
        {
            foreach (var client in Players)
            {
                if (client.Player.IsAlive)
                {
                    return false;
                }
            }

            return true;
        }
    }
}