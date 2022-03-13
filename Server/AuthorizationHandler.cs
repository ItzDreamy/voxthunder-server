using System;
using MySql.Data.MySqlClient;
using System.Data;
using Serilog;

namespace VoxelTanksServer
{
    internal static class AuthorizationHandler
    {
        public static bool ClientAuthRequest(string? username, string? password, string? ip, int playerId,
            out string? message)
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
                Database db = new();
                MySqlCommand myCommand =
                    new(
                        $"SELECT Count(*) FROM `authdata` WHERE `login` = '{username}' AND `password` = '{password}'",
                        db.GetConnection());
                MySqlDataAdapter adapter = new();
                DataTable table = new();
                adapter.SelectCommand = myCommand;
                adapter.Fill(table);
                if (table.Rows[0][0].ToString() == "1")
                {
                    Log.Information($"[{ip}] {username} успешно зашел в аккаунт");
                    message = "Авторизация прошла успешно";
                    Server.Clients[playerId].IsAuth = true;
                    return true;
                }

                Log.Information($"[{ip}] {username} ввел некорректные данные.");
                message = $"Неправильный логин или пароль";
                return false;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            return false;
        }
    }
}