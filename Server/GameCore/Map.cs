namespace VoxelTanksServer.GameCore
{
    /// <summary>
    /// Хранение данных карты
    /// </summary>
    public struct Map
    {
        //Название карты
        public string? Name;

        //Спавнпоинты первой команды
        public readonly List<SpawnPoint> FirstTeamSpawns;

        //Спавнпоинты второй команды
        public readonly List<SpawnPoint> SecondTeamSpawns;

        public Map(string? name, List<SpawnPoint> firstTeamSpawns, List<SpawnPoint> secondTeamSpawns)
        {
            Name = name;
            FirstTeamSpawns = firstTeamSpawns;
            SecondTeamSpawns = secondTeamSpawns;
        }
    }
}