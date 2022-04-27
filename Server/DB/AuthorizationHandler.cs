using System.Data;
using MySql.Data.MySqlClient;
using Serilog;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.DB
{
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
                    
                    var samePlayer = Server.Clients.Values.ToList().Find(player => player?.Username?.ToLower() == username.ToLower());
                    samePlayer?.Disconnect("Другой игрок зашел в аккаунт");
                    
                    Server.Clients[clientId].Username = table.Rows[0][0].ToString();
                    Server.Clients[clientId].IsAuth = true;
                    if (rememberUser)
                    {
                        Guid guid = Guid.NewGuid();
                        db = new Database();
                        db.GetConnection().Open();
                        myCommand = new(
                            $"UPDATE `authdata` SET `authId` = '{guid.ToString()}' WHERE `login` = '{nickname}' AND `password` = '{password}'",
                            db.GetConnection());
                        await myCommand.ExecuteNonQueryAsync();
                        await db.GetConnection().CloseAsync();
                        ServerSend.SendAuthId(guid.ToString(), clientId);
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

            return false;
        }
    }
}