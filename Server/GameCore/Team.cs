using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public class Team {
    public readonly byte Id;
    public readonly List<Client?> Players = new();
    public readonly List<SpawnPoint> SpawnPoints;

    public Team(byte id, List<SpawnPoint> spawnPoints) {
        Id = id;
        SpawnPoints = spawnPoints;
    }

    public bool PlayersDeadCheck() {
        return Players.All(client => client?.Player is not {IsAlive: true});
    }
}