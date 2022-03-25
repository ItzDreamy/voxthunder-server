namespace VoxelTanksServer
{
    /// <summary>
    /// Класс для хранений данных о команде
    /// </summary>
    public class Team
    {
        //Все игроки команды
        public List<Client?> Players = new();
        //Спавнпоинты команды
        public List<SpawnPoint> SpawnPoints;
        //ID команды
        public byte ID;
        
        public Team(byte id, List<SpawnPoint> spawnPoints)
        {
            ID = id;
            SpawnPoints = spawnPoints;
        }

        /// <summary>
        /// Проверка: жива ли команда
        /// </summary>
        /// <returns>Жива ли команда</returns>
        public bool PlayersAliveCheck() 
        {
            foreach (var client in Players)
            {
                if (!client.Player.IsAlive)
                {
                    return false;
                }
            }

            return true;
        }
    }
}