using System.Data;
using MySql.Data.MySqlClient;
using Serilog;
using VoxelTanksServer.DB;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Library;

public static class DatabaseUtils
{
    public static async Task<DataTable> RequestData(string sql)
    {
        var db = new Database();
        MySqlCommand myCommand = new(sql,
            db.GetConnection());
        MySqlDataAdapter adapter = new();
        DataTable table = new();
        adapter.SelectCommand = myCommand;
        await adapter.FillAsync(table);
        return table;
    }

    public static async Task ExecuteNonQuery(string sql)
    {
        Database db = new Database();
        db.GetConnection().Open();
        MySqlCommand myCommand = new(sql
            ,
            db.GetConnection());
        await myCommand.ExecuteNonQueryAsync();
        await db.GetConnection().CloseAsync();
    }

    public static async Task<PlayerStats> GetPlayerStats(string? nickname)
    {
        var stats = new PlayerStats();
        Log.Debug(nickname);
        DataTable table = await RequestData($"SELECT * FROM `playerstats` WHERE `nickname` = '{nickname}'");
        try
        {
            stats.Rank = RankedSystemUtils.GetRank((int) table.Rows[0][2]);
            stats.Battles = (int) table.Rows[0][3];
            stats.WinRate = (float) table.Rows[0][4];
            stats.AvgDamage = (int) table.Rows[0][5];
            stats.AvgKills = (int) table.Rows[0][6];
            stats.AvgExperience = (int) table.Rows[0][7];
            stats.Damage = (int) table.Rows[0][8];
            stats.Kills = (int) table.Rows[0][9];
            stats.Wins = (int) table.Rows[0][10];
            stats.Draws = (int) table.Rows[0][11];
            stats.Loses = (int) table.Rows[0][12];
            stats.Balance = (int) table.Rows[0][13];
            stats.Experience = (int) table.Rows[0][16];

            Console.WriteLine((int) table.Rows[0][2]);
            
            return stats;
        }
        catch (Exception exception)
        {
            Log.Error(exception.ToString());
        }

        return stats;
    }

    public static async Task UpdatePlayerStats(PlayerStats stats, string nickname)
    {
        var (battles, damage, kills, wins, loses, draws, winRate, avgDamage, avgKills, avgExperience, balance,
            experience, rank) = stats;
        await ExecuteNonQuery(
            $"UPDATE `playerstats` SET `battles` = '{battles}', `winrate` = '{winRate}', `avgdamage` = '{avgDamage}', `avgkills` = '{avgKills}', `avgExp` = '{avgExperience}',`damage` = '{damage}', `kills` = '{kills}', `wins` = '{wins}', `loses` = '{loses}', `draws` = '{draws}', `balance` = '{balance}', `exp` = '{experience}', `rankID` = '{rank.Id}' WHERE `nickname` = '{nickname}'");
    }

    public static async Task GenerateAuthToken(string nickname, int clientId)
    {
        Guid guid = Guid.NewGuid();
        await ExecuteNonQuery($"UPDATE `authdata` SET `authId` = '{guid.ToString()}' WHERE `login` = '{nickname}'");
        ServerSend.SendAuthId(guid.ToString(), clientId);
    }

    public static async Task<bool> TryLoginByToken(string authToken, int clientId)
    {
        var table = await RequestData($"SELECT Count(*) FROM `authdata` WHERE `authId` = '{authToken}'");

        if ((long) table.Rows[0][0] > 0)
        {
            table = await RequestData($"SELECT `login` FROM `authdata` WHERE `authId` = '{authToken}'");

            string nickname = table.Rows[0][0].ToString();
            Log.Information($"{nickname} успешно зашел в аккаунт");
            var samePlayer = Server.Clients.Values.ToList()
                .Find(player => player?.Username?.ToLower() == nickname.ToLower());
            samePlayer?.Disconnect("Другой игрок зашел в аккаунт");
            Server.Clients[clientId].Username = table.Rows[0][0].ToString();
            Server.Clients[clientId].IsAuth = true;
            await GenerateAuthToken(nickname, clientId);
            return true;
        }

        return false;
    }
}