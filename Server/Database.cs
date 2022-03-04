using MySql.Data.MySqlClient;

namespace VoxelTanksServer
{
    internal class Database
    {
        private readonly MySqlConnection _connection =
            new MySqlConnection("Server=host;Port=3306;Database=db;Uid=user;Pwd=pass;Charset=utf8");

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