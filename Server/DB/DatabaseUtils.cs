using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using VoxelTanksServer.Protocol;
using Serilog;

namespace VoxelTanksServer.DB
{
    public static class DatabaseUtils
    {
        public static async Task<PlayerStats> GetPlayerStats(string? nickname)
        {
            var stats = new PlayerStats();

            var db = new Database();

            MySqlCommand myCommand = new($"SELECT * FROM `playerstats` WHERE `nickname` = '{nickname}'",
                db.GetConnection());
            MySqlDataAdapter adapter = new();
            DataTable table = new();
            adapter.SelectCommand = myCommand;
            await adapter.FillAsync(table);

            try
            {
                stats.Battles = (int) table.Rows[0][2];
                stats.WinRate = (float) table.Rows[0][3];
                stats.AvgDamage = (int) table.Rows[0][4];
                stats.AvgKills = (int) table.Rows[0][5];
                stats.Damage = (int) table.Rows[0][6];
                stats.Kills = (int) table.Rows[0][7];
                stats.Wins = (int) table.Rows[0][8];
                stats.Draws = (int) table.Rows[0][9];
                stats.Loses = (int) table.Rows[0][10];
                stats.Balance = (int) table.Rows[0][11];

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
            var (battles, damage, kills, wins, loses, draws, winRate, avgDamage, avgKills, balance) = stats;

            Database db = new Database();
            db.GetConnection().Open();
            MySqlCommand myCommand = new(
                $"UPDATE `playerstats` SET `battles` = '{battles}', `winrate` = '{winRate}', `avgdamage` = '{avgDamage}', `avgkills` = '{avgKills}', `damage` = '{damage}', `kills` = '{kills}', `wins` = '{wins}', `loses` = '{loses}', `draws` = '{draws}', `balance` = '{balance}' WHERE `nickname` = '{nickname}'",
                db.GetConnection());
            await myCommand.ExecuteNonQueryAsync();
            await db.GetConnection().CloseAsync();
        }

        public static async Task<bool> TryLoginById(string authId, int clientId)
        {
            string message = "";
            
            var db = new Database();
            var myCommand =
                new MySqlCommand($"SELECT Count(*) FROM `authdata` WHERE `authId` = '{authId}'",
                    db.GetConnection());
            var adapter = new MySqlDataAdapter();
            var table = new DataTable();
            adapter.SelectCommand = myCommand;
            await adapter.FillAsync(table);

            if ((long) table.Rows[0][0] > 0)
            {
                db = new Database();
                myCommand =
                    new MySqlCommand($"SELECT `login` FROM `authdata` WHERE `authId` = '{authId}'",
                        db.GetConnection());
                adapter = new MySqlDataAdapter();
                table = new DataTable();
                adapter.SelectCommand = myCommand;
                await adapter.FillAsync(table);
                
                string nickname = table.Rows[0][0].ToString();

                Log.Information($"{nickname} успешно зашел в аккаунт");
                message = "Авторизация прошла успешно";
                    
                var samePlayer = Server.Clients.Values.ToList().Find(player => player?.Username?.ToLower() == nickname.ToLower());
                samePlayer?.Disconnect("Другой игрок зашел в аккаунт");
                
                Server.Clients[clientId].Username = table.Rows[0][0].ToString();
                Server.Clients[clientId].IsAuth = true;
                
                ServerSend.LoginResult(clientId, true, message);
                return true;
            }

            message = "Недействительный id авторизации";
            ServerSend.LoginResult(clientId, false, message);
            
            return false;
        }
    }
}