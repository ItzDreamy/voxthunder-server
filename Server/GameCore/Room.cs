using VoxelTanksServer.Library;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public class Room {
    private readonly int _generalTime;

    private readonly int _playersPerTeam;

    public readonly List<CachedPlayer?> CachedPlayers = new();
    public readonly Map Map;

    public readonly Dictionary<int, Client?> Players = new();

    private int _currentTime;

    private bool _timerRunning;
    public bool GameEnded;

    public bool IsOpen = true;

    public bool PlayersLocked = true;

    public Room(int generalTime, int preparativeTime) {
        MaxPlayers = Server.Config.MaxPlayersInRoom;
        _playersPerTeam = MaxPlayers / 2;
        _generalTime = generalTime;
        PreparationTime = preparativeTime;

        Map = Server.Maps[new Random().Next(Server.Maps.Count)];

        var firstTeamSpawns = Map.FirstTeamSpawns.Select(point => (SpawnPoint) point.Clone()).ToList();
        var secondTeamSpawns = Map.SecondTeamSpawns.Select(point => (SpawnPoint) point.Clone()).ToList();

        Teams = new List<Team?> {
            new(1, firstTeamSpawns), new(2, secondTeamSpawns)
        };

        Server.Rooms.Add(this);
    }

    public int PreparationTime { get; }

    public int MaxPlayers { get; }
    public List<Team?> Teams { get; }
    public int PlayersCount => Players.Count;

    public void BalanceTeams() {
        foreach (var client in Players.Values) {
            var randomTeam = 0;
            Team? playerTeam;

            do {
                randomTeam = new Random().Next(1, 3);
                playerTeam = Teams.Find(team => team != null && team.Id == randomTeam);
            } while (playerTeam != null && playerTeam.Players.Count == _playersPerTeam);

            playerTeam?.Players.Add(client);
            client.Team = playerTeam;

            var openPoints = playerTeam.SpawnPoints.FindAll(point => point.IsOpen);
            var pointIndex = new Random().Next(openPoints.Count);
            var point = openPoints[pointIndex];
            point.IsOpen = false;

            client.SpawnPosition = point.Position;
            client.SpawnRotation = point.Rotation;
        }

        ServerSend.LoadScene(this, Map.Name);

        Task.Run(async () => {
            var waitingTime = 60000;

            while (waitingTime > 0) {
                if (CheckPlayersReady()) {
                    foreach (var client in Players.Values)
                        client?.SendIntoGame(client.Data.Username, client.SelectedTank);

                    StartTimer(Timers.Preparative, PreparationTime);
                    return;
                }

                waitingTime -= 1000;
                await Task.Delay(1000);
            }

            foreach (var client in Players.Values) {
                client.LeaveRoom();
                ServerSend.LeaveToLobby(client.Id);
            }
        });
    }

    private bool CheckPlayersReady() {
        foreach (var client in Players.Values)
            if (!client.ReadyToSpawn)
                return false;

        return true;
    }

    public void StartTimer(Timers type, int time) {
        if (_timerRunning)
            return;

        _currentTime = time;
        _timerRunning = true;
        Task.Run(async () => {
            while (_currentTime > 0 && !GameEnded) {
                _currentTime -= 1000;
                ServerSend.SendTimer(this, _currentTime, type == Timers.General);
                await Task.Delay(1000);
            }

            if (type == Timers.General) {
                if (!GameEnded) {
                    GameEnded = true;

                    ServerSend.SendPlayersStats(this);

                    foreach (var team in Teams) {
                        foreach (var client in team.Players) client.Player.UpdatePlayerStats(GameResults.Draw);
                        ServerSend.EndGame(team, GameResults.Draw);
                    }

                    foreach (var player in Players.Values) player?.LeaveRoom();
                }
            }
            else {
                _timerRunning = false;
                StartTimer(Timers.General, _generalTime);
                ServerSend.UnlockPlayers(this);
                PlayersLocked = false;
                foreach (var player in Players.Values) {
                    player.Player.LastShootedTime = DateTime.Now;
                }
            }
        });
    }
}