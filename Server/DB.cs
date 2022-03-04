using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTanksServer
{
    internal class DB
    {
        MySqlConnection connection = new MySqlConnection("Server=host;Port=3306;Database=db;Uid=user;Pwd=pass;Charset=utf8");

        public void OpenConnection()
        {
            if (connection.State == System.Data.ConnectionState.Closed)

                connection.Open();


        }
        public void closeConnection()
        {
            if (connection.State == System.Data.ConnectionState.Open)

                connection.Close();

        }

        public MySqlConnection GetConnection()
        {
            return connection;
        }
    }
}
