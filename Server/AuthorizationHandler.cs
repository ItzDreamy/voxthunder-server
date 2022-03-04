using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTanksServer
{
    internal class AuthorizationHandler
    {
        private void ClientAuthRequest()
        {
            Database db = new Database();
            MySqlCommand myCommand = new MySqlCommand("SELECT Count(*) FROM  admin WHERE login = 0" + "логин" + "' AND password = '" + "пароль в мд5" + "'", db.GetConnection());
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            DataTable table = new DataTable();
            adapter.SelectCommand = myCommand;
            adapter.Fill(table);
            if (table.Rows[0][0].ToString() == "1")
            {
                //отправлять клиенту что авторизация успешна
            }
            else
            {
                //посылать клиента нахрен т.к авторизация неудачна
            }
        }
    }
}
