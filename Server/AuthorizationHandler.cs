using System;
using MySql.Data.MySqlClient;
using System.Data;

namespace VoxelTanksServer
{
    internal class AuthorizationHandler
    {
        public static bool ClientAuthRequest(string username, string password)
        {
            try
            {
                Database db = new Database();
                MySqlCommand myCommand =
                    new MySqlCommand(
                        $"SELECT Count(*) FROM `authdata` WHERE `login` = '{username}' AND `password` = '{password}'" , db.GetConnection());
                MySqlDataAdapter adapter = new MySqlDataAdapter();
                DataTable table = new DataTable();
                adapter.SelectCommand = myCommand;
                adapter.Fill(table);
                return table.Rows[0][0].ToString() == "1";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }
    }
}