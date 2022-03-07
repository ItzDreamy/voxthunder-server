using System;
using MySql.Data.MySqlClient;
using System.Data;
using Serilog;
using Serilog.Core;

namespace VoxelTanksServer
{
    internal static class AuthorizationHandler
    {
        public static bool ClientAuthRequest(string username, string password, string? ip, int playerId,
            out string message)
        {
            message = "";
            foreach (var client in Server.Clients.Values)
            {
                if (client.Username == username)
                {
                    Log.Information($"[{ip}] Игрок с логином {username} уже в сети!");
                    message = $"Игрок с логином {username} уже в сети!";
                    return false;
                }
            }

            try
            {
                Database db = new Database();
                MySqlCommand myCommand =
                    new MySqlCommand(
                        $"SELECT Count(*) FROM `authdata` WHERE `login` = '{username}' AND `password` = '{password}'",
                        db.GetConnection());
                MySqlDataAdapter adapter = new MySqlDataAdapter();
                DataTable table = new DataTable();
                adapter.SelectCommand = myCommand;
                adapter.Fill(table);
                if (table.Rows[0][0].ToString() == "1")
                {
                    Log.Information($"[{ip}] {username} успешно зашел в аккаунт");
                    message = "Авторизация прошла успешно";

                    return true;
                }

                Log.Information($"[{ip}] {username} ввел некорректные данные.");
                message = $"Неправильный логин или пароль";
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }
    }
}