using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
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
                throw;
            }
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
    }
}