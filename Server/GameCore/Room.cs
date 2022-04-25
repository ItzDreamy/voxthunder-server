using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoxelTanksServer.Library;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore
{
    public class Room
    {
        public bool GameEnded = false;

        public bool IsOpen = true;

        private bool _timerRunning = false;

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

        private readonly int _generalTime;

        public Room(int generalTime, int preparativeTime)
        {
            MaxPlayers = Server.Config.MaxPlayersInRoom;
            _playersPerTeam = MaxPlayers / 2;
            _generalTime = generalTime;
            PreparationTime = preparativeTime;

            Map = Server.Maps[new Random().Next(Server.Maps.Count)];

            var firstTeamSpawns = Map.FirstTeamSpawns.Select(point => (SpawnPoint) point.Clone()).ToList();
            var secondTeamSpawns = Map.SecondTeamSpawns.Select(point => (SpawnPoint) point.Clone()).ToList();

            Teams = new List<Team?>
            {
                new(1, firstTeamSpawns), new(2, secondTeamSpawns)
            };

            Server.Rooms.Add(this);
        }

        public void BalanceTeams()
        {
            foreach (var client in Players.Values)
            {
                int randomTeam = 0;
                Team? playerTeam;

                do
                {
                    randomTeam = new Random().Next(1, 3);
                    playerTeam = Teams.Find(team => team != null && team.Id == randomTeam);
                } while (playerTeam != null && playerTeam.Players.Count == _playersPerTeam);

                playerTeam?.Players.Add(client);
                client.Team = playerTeam;

                var openPoints = playerTeam.SpawnPoints.FindAll(point => point.IsOpen);
                int pointIndex = new Random().Next(openPoints.Count);
                var point = openPoints[pointIndex];
                point.IsOpen = false;

                client.SpawnPosition = point.Position;
                client.SpawnRotation = point.Rotation;
            }

            ServerSend.LoadScene(this, Map.Name);

            Task.Run(async () =>
            {
                int waitingTime = 60000;

                while (waitingTime > 0)
                {
                    if (CheckPlayersReady())
                    {
                        foreach (var client in Players.Values)
                        {
                            client?.SendIntoGame(client.Username, client.SelectedTank);
                        }

                        StartTimer(Timers.Preparative, PreparationTime);
                        return;
                    }

                    waitingTime -= 1000;
                    await Task.Delay(1000);
                }

                foreach (var client in Players.Values)
                {
                    client.LeaveRoom();
                    ServerSend.LeaveToLobby(client.Id);
                }
            });
        }

        private bool CheckPlayersReady()
        {
            foreach (var client in Players.Values)
            {
                if (!client.ReadyToSpawn)
                {
                    return false;
                }
            }

            return true;
        }

        public void StartTimer(Timers type, int time)
        {
            if (_timerRunning)
                return;

            _currentTime = time;
            _timerRunning = true;
            Task.Run(async () =>
            {
                while (_currentTime > 0 && !GameEnded)
                {
                    _currentTime -= 1000;
                    ServerSend.SendTimer(this, _currentTime, type == Timers.General);
                    await Task.Delay(1000);
                }

                if (type == Timers.General)
                {
                    if (!GameEnded)
                    {
                        GameEnded = true;

                        ServerSend.SendPlayersStats(this);

                        foreach (var team in Teams)
                        {
                            foreach (var client in team.Players)
                            {
                                client.Player.UpdatePlayerStats(GameResults.Draw);
                            }
                            ServerSend.EndGame(team, GameResults.Draw);
                        }

                        foreach (var player in Players.Values)
                        {
                            player?.LeaveRoom();
                        }
                    }
                }
                else
                {
                    _timerRunning = false;
                    StartTimer(Timers.General, _generalTime);
                    ServerSend.UnlockPlayers(this);
                    PlayersLocked = false;
                }
            });
        }
    }
}