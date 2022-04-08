using System.Collections.Generic;
using System.Linq;

namespace VoxelTanksServer
{
    /// <summary>
    /// Класс для хранений данных о команде
    /// </summary>
    public class Team
    {
        //Все игроки команды
        public readonly List<Client?> Players = new();
        //Спавнпоинты команды
        public readonly List<SpawnPoint> SpawnPoints;
        //ID команды
        public readonly byte Id;
        
        public Team(byte id, List<SpawnPoint> spawnPoints)
        {
            Id = id;
            SpawnPoints = spawnPoints;
        }

        /// <summary>
        /// Проверка: мертва ли команда
        /// </summary>
        /// <returns>Мертва ли команда</returns>
        public bool PlayersDeadCheck()
        {
            return Players.All(client => client?.Player is not {IsAlive: true});
        }
    }
}