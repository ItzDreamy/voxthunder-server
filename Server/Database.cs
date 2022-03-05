using MySql.Data.MySqlClient;

namespace VoxelTanksServer
{
    internal class Database
    {
        private readonly MySqlConnection _connection =
            new MySqlConnection("Server=31.31.198.105;Port=3306;Database=u1447827_default;Uid=u1447827;Pwd=uE0wA7oI4rvX4e;Charset=utf8");

        public void OpenConnection()
        {
            if (_connection.State == System.Data.ConnectionState.Closed)
                _connection.Open();
        }

        public void CloseConnection()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
        }

        public MySqlConnection GetConnection()
        {
            return _connection;
        }
    }
}