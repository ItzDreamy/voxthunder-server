using Newtonsoft.Json;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Library.LevelingSystem;

public static class Leveling {
    public static Rank MaxRank {
        get {
            var jsonRanks = File.ReadAllText("Library/LevelingSystem/ranks.json");
            var ranks = JsonConvert.DeserializeObject<Rank[]>(jsonRanks);
            var rankId = ranks.ToList().Select(rank => rank.Id).Max();
            return GetRank(rankId);
        }
    }

    public static Rank GetRank(int id) {
        var jsonRanks = File.ReadAllText("Library/LevelingSystem/ranks.json");
        var ranks = JsonConvert.DeserializeObject<Rank[]>(jsonRanks);
        return ranks.ToList().Find(rank => rank.Id == id);
    }

    public static Rank GetRank(string rankName) {
        var jsonRanks = File.ReadAllText("Library/LevelingSystem/ranks.json");
        var ranks = JsonConvert.DeserializeObject<Rank[]>(jsonRanks);
        return ranks.ToList().Find(rank => rank.Name == rankName);
    }

    public static async Task<Rank> GetRank(Client client) {
        var table = await DatabaseUtils.RequestData(
            $"SELECT `rankID` FROM `playerstats` WHERE `nickname` = '{client.Data.Username}'");

        return GetRank((int) table.Rows[0][0]);
    }

    public static async Task<int> GetCurrentExp(string username) {
        var table = await DatabaseUtils.RequestData(
            $"SELECT `exp` FROM `playerstats` WHERE `nickname` = '{username}'");

        return (int) table.Rows[0][0];
    }

    public static bool CheckRankUp(Client client, out Rank nextRank) {
        if (client.Data.Rank.Id == MaxRank.Id) {
            nextRank = default;
            return false;
        }

        nextRank = GetRank(client.Data.Rank.Id + 1);
        return client.Data.Experience >= nextRank.RequiredExp;
    }
}