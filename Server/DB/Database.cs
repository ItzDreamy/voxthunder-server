using MySql.Data.MySqlClient;

namespace VoxelTanksServer.DB
{
    internal class Database
    {
        private readonly MySqlConnection _connection =
            new("Server=31.31.198.105;Port=3306;Database=u1447827_default;Uid=u1447827;Pwd=uE0wA7oI4rvX4e;Charset=utf8");

        public MySqlConnection GetConnection()
        {
            return _connection;
        }
    }
}