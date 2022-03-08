using System.Data;
using MySql.Data.MySqlClient;

namespace VoxelTanksServer
{
    internal class Database
    {
        private readonly MySqlConnection _connection =
            new MySqlConnection("Server=31.31.198.105;Port=3306;Database=u1447827_default;Uid=u1447827;Pwd=uE0wA7oI4rvX4e;Charset=utf8");

        public MySqlConnection GetConnection()
        {
            return _connection;
        }

        public static object RequestData(string neededColumn, string tableName, string column, string columnValue)
        {
            var db = new Database();
            MySqlCommand myCommand =
                new MySqlCommand(
                    $"SELECT `{neededColumn}` FROM `{tableName}` WHERE `{column}` = '{columnValue}'",
                    db.GetConnection());
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            DataTable table = new DataTable();
            adapter.SelectCommand = myCommand;
            adapter.Fill(table);
            return table.Rows[0][0];
        }
    }
}