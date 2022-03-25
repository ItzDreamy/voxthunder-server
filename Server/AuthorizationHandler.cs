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
            message = null;
            return false;
        }
    }
}