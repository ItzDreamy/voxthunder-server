using System.Data;
using MySql.Data.MySqlClient;
using Serilog;
using VoxelTanksServer.DB;
using VoxelTanksServer.Library.LevelingSystem;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Library;

public static class DatabaseUtils {
    public static async Task<DataTable> RequestData(string sql) {
        var db = new Database();
        MySqlCommand myCommand = new(sql,
            db.GetConnection());
        MySqlDataAdapter adapter = new();
        DataTable table = new();
        adapter.SelectCommand = myCommand;
        await adapter.FillAsync(table);
        return table;
    }

    public static async Task ExecuteNonQuery(string sql) {
        var db = new Database();
        db.GetConnection().Open();
        MySqlCommand myCommand = new(sql
            ,
            db.GetConnection());
        await myCommand.ExecuteNonQueryAsync();
        await db.GetConnection().CloseAsync();
    }

    public static async Task<PlayerData> GetPlayerData(Client client) {
        var data = client.Data;
        var table = await RequestData($"SELECT * FROM `playerstats` WHERE `nickname` = '{client.Data.Username}'");
        try {
            data.Rank = Leveling.GetRank((int) table.Rows[0][2]);
            data.Battles = (int) table.Rows[0][3];
            data.WinRate = (float) table.Rows[0][4];
            data.AvgDamage = (int) table.Rows[0][5];
            data.AvgKills = (int) table.Rows[0][6];
            data.AvgExperience = (int) table.Rows[0][7];
            data.Damage = (int) table.Rows[0][8];
            data.Kills = (int) table.Rows[0][9];
            data.Wins = (int) table.Rows[0][10];
            data.Draws = (int) table.Rows[0][11];
            data.Loses = (int) table.Rows[0][12];
            data.Balance = (int) table.Rows[0][13];
            data.Experience = (int) table.Rows[0][17];
            return data;
        }
        catch (Exception exception) {
            Log.Error(exception.ToString());
        }

        return data;
    }

    public static async Task UpdatePlayerData(PlayerData data) {
        var (username, battles, damage, kills, wins, loses, draws, winRate, avgDamage, avgKills, avgExperience, balance,
            experience, rank) = data;
        await ExecuteNonQuery(
            $"UPDATE `playerstats` SET `battles` = '{battles}', `winrate` = '{winRate}', `avgdamage` = '{avgDamage}', `avgkills` = '{avgKills}', `avgExp` = '{avgExperience}',`damage` = '{damage}', `kills` = '{kills}', `wins` = '{wins}', `loses` = '{loses}', `draws` = '{draws}', `balance` = '{balance}', `exp` = '{experience}', `rankID` = '{rank.Id}' WHERE `nickname` = '{username}'");
    }

    public static async Task GenerateAuthToken(string nickname, int clientId) {
        var guid = Guid.NewGuid();
        await ExecuteNonQuery($"UPDATE `authdata` SET `authId` = '{guid.ToString()}' WHERE `login` = '{nickname}'");
        ServerSend.SendAuthId(guid.ToString(), clientId);
    }

    public static async Task<bool> TryLoginByToken(string authToken, int clientId) {
        var table = await RequestData($"SELECT Count(*) FROM `authdata` WHERE `authId` = '{authToken}'");

        if ((long) table.Rows[0][0] > 0) {
            table = await RequestData($"SELECT `login` FROM `authdata` WHERE `authId` = '{authToken}'");

            var nickname = table.Rows[0][0].ToString();
            Log.Information($"{nickname} успешно зашел в аккаунт");
            
            var samePlayer = Server.Clients.Values.ToList()
                .Find(player => player?.Data.Username?.ToLower() == nickname.ToLower());
            samePlayer?.Disconnect("Другой игрок зашел в аккаунт");
            
            Server.Clients[clientId].Data.Username = nickname;
            Server.Clients[clientId].IsAuth = true;
            await GenerateAuthToken(nickname, clientId);
            return true;
        }

        return false;
    }
}