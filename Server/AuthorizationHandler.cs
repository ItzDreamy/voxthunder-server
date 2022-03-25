using MySql.Data.MySqlClient;
using System.Data;
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
        /// <param name="playerId">ID игрока</param>
        /// <param name="message">Сообщение возвращаемое игроку</param>
        /// <returns>Булевый результат операции</returns>
        public static bool ClientAuthRequest(string? username, string? password, string? ip, int playerId,
            out string? message)
        {
            message = "";

            //Проверка на наличие игрока с таким же ником
            foreach (var client in Server.Clients.Values)
            {
                if (client.Username?.ToLower() == username.ToLower())
                {
                    Log.Information($"[{ip}] Игрок с логином {client.Username} уже в сети!");
                    message = $"Игрок с логином {username} уже в сети!";
                    return false;
                }
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
                Task.Run(async () => await adapter.FillAsync(table));

                try
                {
                    //Если игрок с такими ником и паролем существует, то запускать в игру
                    string nickname = table.Rows[0][0].ToString();

                    Log.Information($"[{ip}] {nickname} успешно зашел в аккаунт");
                    message = "Авторизация прошла успешно";
                    Server.Clients[playerId].Username = table.Rows[0][0].ToString();
                    Server.Clients[playerId].IsAuth = true;
                    return true;

                }
                catch (Exception ex)
                {
                    //Иначе говорить игроку, что данные некорректные
                    Log.Information($"[{ip}] {username} ввел некорректные данные.");
                    message = $"Неправильный логин или пароль";
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            return false;
        }
    }
}