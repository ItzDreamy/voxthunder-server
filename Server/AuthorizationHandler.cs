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
        private void clientAuthRequest()
        {
            DB db = new DB();
            MySqlCommand myCommand = new MySqlCommand("SELECT Count(*) FROM  admin WHERE login = '" + "логин" + "' AND password = '" + "пароль в мд5" + "'", db.GetConnection());
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            DataTable dt = new DataTable();
            adapter.SelectCommand = myCommand;
            adapter.Fill(dt);
            if (dt.Rows[0][0].ToString() == "1")
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
