using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoxelTanksServer
{
    /// <summary>
    /// Игровая комната
    /// </summary>
    public class Room
    {
        public bool GameEnded = false;
        
        public bool IsOpen = true;

        public bool PlayersLocked = true;

        public int PreparationTime { get; }

        public int MaxPlayers { get; private set; }
        public List<Team?> Teams { get; private set; }
        public readonly Map Map;
        public int PlayersCount => Players.Count;

        public readonly Dictionary<int, Client?> Players = new();

        public readonly List<CachedPlayer?> CachedPlayers = new();

        private readonly int _playersPerTeam;

        private int _currentTime;

        private int _generalTime;

        /// <summary>
        /// Создание новой комнаты
        /// </summary>
        /// <param name="maxPlayers">Кол-во игроков в комнате</param>
        /// <param name="generalTime">Основное время в игре (в миллисекундах)</param>
        /// <param name="preparativeTime">Подготовительное время (в миллисекундах)</param>
        public Room(int maxPlayers, int generalTime, int preparativeTime)
        {
            MaxPlayers = maxPlayers;
            _playersPerTeam = 1;
            _generalTime = generalTime;
            PreparationTime = preparativeTime;

            //Выбор случайной карты
            Map = Server.Maps[new Random().Next(Server.Maps.Count)];

            //Инициализация спавнпоинтов для команд
            var firstTeamSpawns = Map.FirstTeamSpawns.Select(point => (SpawnPoint) point.Clone()).ToList();
            var secondTeamSpawns = Map.SecondTeamSpawns.Select(point => (SpawnPoint) point.Clone()).ToList();

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
                    playerTeam = Teams.Find(team => team != null && team.Id == randomTeam);
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

        public void StartTimer(Server.Timers type, int time)
        {
            _currentTime = time;
            Task.Run(async () =>
            {
                while (_currentTime > 0 && !GameEnded)
                {
                    _currentTime -= 1000;
                    ServerSend.SendTimer(this, _currentTime, type == Server.Timers.General);
                    await Task.Delay(1000);
                }
                
                if (type == Server.Timers.General)
                {
                    if (!GameEnded)
                    {
                        GameEnded = true;
                
                        ServerSend.SendPlayersStats(this);
                
                        foreach (var team in Teams)
                        {
                            ServerSend.EndGame(team, false, true);
                        }
                    
                        foreach (var player in Players.Values)
                        {
                            player?.LeaveRoom();
                        }
                    }
                }
                else
                {
                    StartTimer(Server.Timers.General, _generalTime);
                    Console.WriteLine("Players unlocked");
                    ServerSend.UnlockPlayers(this);
                    PlayersLocked = false;
                }
            });
        }
    }
}