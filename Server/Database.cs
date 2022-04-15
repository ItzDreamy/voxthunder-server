using MySql.Data.MySqlClient;

namespace VoxelTanksServer
{
    internal class Database
    {
        //Данные о подключении к БД
        private readonly MySqlConnection _connection =
            new("Server=31.31.198.105;Port=3306;Database=u1447827_default;Uid=u1447827;Pwd=uE0wA7oI4rvX4e;Charset=utf8");

        //Получение подключения к БД
        public MySqlConnection GetConnection()
        {
            return _connection;
        }

        //Метод пока что не юзается...
        // public static object RequestData(string neededColumn, string tableName, string column, string columnValue)
        // {
        //     var db = new Database();
        //     MySqlCommand myCommand =
        //         new(
        //             $"SELECT `{neededColumn}` FROM `{tableName}` WHERE `{column}` = '{columnValue}'",
        //             db.GetConnection());
        //     MySqlDataAdapter adapter = new();
        //     DataTable table = new();
        //     adapter.SelectCommand = myCommand;
        //     adapter.Fill(table);
        //     return table.Rows[0][0];
        // }
    }
}