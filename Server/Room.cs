using System;
using System.Collections.Generic;

namespace VoxelTanksServer
{
    /// <summary>
    /// Игровая комната
    /// </summary>
    public class Room
    {
        public bool IsOpen = true;
        public int MaxPlayers { get; private set; }
        public List<Team?> Teams { get; private set; }
        public readonly Map Map;
        public int PlayersCount => Players.Count;

        public readonly Dictionary<int, Client?> Players = new();

        public readonly List<CachedPlayer?> CachedPlayers = new();

        private int _playersPerTeam;

        public Room(int maxPlayers)
        {
            MaxPlayers = maxPlayers;
            _playersPerTeam = 1;

            //Выбор случайной карты
            Map = Server.Maps[new Random().Next(Server.Maps.Count)];

            //Инициализация спавнпоинтов для команд
            var firstTeamSpawns = new List<SpawnPoint>();
            var secondTeamSpawns = new List<SpawnPoint>();

            foreach (var point in Map.FirstTeamSpawns)
            {
                firstTeamSpawns.Add((SpawnPoint) point.Clone());
            }
            foreach (var point in Map.SecondTeamSpawns)
            {
                secondTeamSpawns.Add((SpawnPoint)point.Clone());
            }

            //Создание команд
            Teams = new List<Team?>
            {
                new(1, firstTeamSpawns), new(2, secondTeamSpawns)
            };
            
            //Добавление комнаты в список комнат сервера
            Server.Rooms.Add(this);
        }

        /// <summary>
        /// Балансировка команд
        /// </summary>
        public void BalanceTeams()
        {
            foreach (var client in Players.Values)
            {
                int randomTeam = 0;
                Team? playerTeam;
                
                //Поиск доступной команды
                do
                {
                    randomTeam = new Random().Next(1, 3);
                    playerTeam = Teams.Find(team => team != null && team.ID == randomTeam);
                } while (playerTeam != null && playerTeam.Players.Count == _playersPerTeam);
                
                playerTeam?.Players.Add(client);
                client.Team = playerTeam;

                //Установка рандомной точки спавна
                var openPoints = playerTeam.SpawnPoints.FindAll(point => point.IsOpen);
                int pointIndex = new Random().Next(openPoints.Count);
                var point = openPoints[pointIndex];
                point.IsOpen = false;

                client.Position = point.Position;
                client.Rotation = point.Rotation;
            }
            //Запуск игры
            ServerSend.LoadScene(this, Map.Name);
        }
    }
}