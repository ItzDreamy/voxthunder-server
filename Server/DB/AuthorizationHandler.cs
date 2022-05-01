using System.Data;
using MySql.Data.MySqlClient;
using Serilog;
using VoxelTanksServer.Library;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.DB;

public static class AuthorizationHandler
{
    public static async Task<bool> TryLogin(string username, string password, bool rememberUser, string ip, int clientId)
    {
        string message = "";
        try
        {
            Database db = new();
            MySqlCommand myCommand =
                new(
                    $"SELECT `login` FROM `authdata` WHERE `login` = '{username}' AND `password` = '{password}'",
                    db.GetConnection());
            MySqlDataAdapter adapter = new();
            DataTable table = new();
            adapter.SelectCommand = myCommand;
            await adapter.FillAsync(table);

            try
            {
                string nickname = table.Rows[0][0].ToString();

                Log.Information($"[{ip}] {nickname} успешно зашел в аккаунт");
                message = "Авторизация прошла успешно";
                    
                var samePlayer = Server.Clients.Values.ToList().Find(player => player?.Data.Username?.ToLower() == username.ToLower());
                samePlayer?.Disconnect("Другой игрок зашел в аккаунт");

                var client = Server.Clients[clientId];
                client.Data.Username = table.Rows[0][0].ToString();
                client.IsAuth = true;

                if (rememberUser)
                {
                    await DatabaseUtils.GenerateAuthToken(nickname, clientId);
                }

                ServerSend.LoginResult(clientId, true, message);

                return true;
            }
            catch (Exception ex)
            {
                Log.Information($"[{ip}] {username} ввел некорректные данные.");
                Log.Error(ex.ToString());
                message = $"Неправильный логин или пароль";
                ServerSend.LoginResult(clientId, false, message);
                return false;
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            ServerSend.LoginResult(clientId, false, message);
            return false;
        }
    }
}