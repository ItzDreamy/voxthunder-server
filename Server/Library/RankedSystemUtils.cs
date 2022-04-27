using System.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using VoxelTanksServer.DB;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Library;
public static class RankedSystemUtils
{
    public static Rank GetRank(int id)
    {
        string jsonRanks = File.ReadAllText("Library/ranks.json");
        Rank[] ranks = JsonConvert.DeserializeObject<Rank[]>(jsonRanks);
        return ranks.ToList().Find(rank => rank.Id == id);
    }

    public static Rank GetRank(string rankName)
    {
        string jsonRanks = File.ReadAllText("Library/ranks.json");
        Rank[] ranks = JsonConvert.DeserializeObject<Rank[]>(jsonRanks);
        return ranks.ToList().Find(rank => rank.Name == rankName);
    }

    public static async Task<Rank> GetRank(Client client)
    {
        var db = new Database();

        MySqlCommand myCommand = new($"SELECT `rankID` FROM `playerstats` WHERE `nickname` = '{client.Username}'",
            db.GetConnection());
        MySqlDataAdapter adapter = new();
        DataTable table = new();
        adapter.SelectCommand = myCommand;
        await adapter.FillAsync(table);

        return GetRank((int) table.Rows[0][0]);
    }

    public static async Task<bool> CheckRankUp(Client client)
    {
        var db = new Database();
        
        MySqlCommand myCommand = new($"SELECT `exp` FROM `playerstats` WHERE `nickname` = '{client.Username}'",
            db.GetConnection());
        MySqlDataAdapter adapter = new();
        DataTable table = new();
        adapter.SelectCommand = myCommand;
        await adapter.FillAsync(table);
        
        int exp = (int) table.Rows[0][0];
        Rank currentRank = await GetRank(client);
        Rank nextRank = GetRank(currentRank.Id + 1);
        
        return exp >= nextRank.RequiredExp;
    }
}