using System;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace VoxelTanksServer
{
    public static class AuthorizationHandler
    {
        /// <summary>
        /// Запрос для проверки корректности логина и пароля
        /// </summary>
        /// <param name="username">Логин игрока</param>
        /// <param name="password">Пароль игрока</param>
        /// <param name="ip">Адрес игрока</param>
        /// <param name="clientId">ID игрока</param>
        /// <returns>Булевый результат операции</returns>
        public static async Task<bool> TryLogin(string username, string password, string ip, int clientId)
        {
            string message = "";

            //Проверка на наличие игрока с таким же ником
            var samePlayer = Server.Clients.Values.ToList().Find(player => player?.Username?.ToLower() == username.ToLower());
            if (samePlayer != null)
            {
                Log.Information($"[{ip}] Игрок с логином {samePlayer.Username} уже в сети!");
                message = $"Игрок с логином {username} уже в сети!";
                ServerSend.LoginResult(clientId, false, message);
                return false;
            }

            try
            {
                //Создание запроса к БД
                Database db = new();
                MySqlCommand myCommand =
                    new(
                        $"SELECT `login` FROM `authdata` WHERE `login` = '{username}' AND `password` = '{password}'",
                        db.GetConnection());
                MySqlDataAdapter adapter = new();
                DataTable table = new();
                adapter.SelectCommand = myCommand;

                //Ассинхронный запрос в БД для того, что бы не блокировался поток сервера
                await adapter.FillAsync(table);
                try
                {
                    //Если игрок с такими ником и паролем существует, то запускать в игру
                    string nickname = table.Rows[0][0].ToString();

                    Log.Information($"[{ip}] {nickname} успешно зашел в аккаунт");
                    message = "Авторизация прошла успешно";
                    Server.Clients[clientId].Username = table.Rows[0][0].ToString();
                    Server.Clients[clientId].IsAuth = true;
                    ServerSend.LoginResult(clientId, true, message);
                    return true;

                }
                catch (Exception ex)
                {
                    //Иначе говорить игроку, что данные некорректные
                    Log.Information($"[{ip}] {username} ввел некорректные данные.");
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
}