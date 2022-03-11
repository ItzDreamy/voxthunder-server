using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using Serilog;

namespace VoxelTanksServer
{
    public class Tank
    {
        public bool Initialized = false;

        public string Name;
        public int Damage;
        public int MaxHealth;
        public float TowerRotateSpeed;
        public float TankRotateSpeed;
        public float AngleUp;
        public float AngleDown;
        public float MaxSpeed;
        public float Acceleration;
        public float MaxBackSpeed;
        public float BackAcceleration;
        public float Cooldown;

        public Tank(string name)
        {
            Log.Debug("Start initialize");
            Name = name;
            Thread databaseThread = new(GetStats);
            databaseThread.Start();
        }

        private void GetStats()
        {
            while (true)
            {
                var db = new Database();
                MySqlCommand myCommand =
                    new(
                        $"SELECT * FROM `tanksstats` WHERE `tankname` = '{Name.ToLower()}'",
                        db.GetConnection());
                MySqlDataAdapter adapter = new();
                DataTable table = new();
                adapter.SelectCommand = myCommand;
                adapter.Fill(table);
                Damage = (int) table.Rows[0][2];
                MaxHealth = (int) table.Rows[0][3];
                TowerRotateSpeed =
                    (float) table.Rows[0][4];
                TankRotateSpeed = (float) table.Rows[0][5];
                AngleUp = (float) table.Rows[0][6];
                AngleDown = (float) table.Rows[0][7];
                MaxSpeed = (float) table.Rows[0][8];
                Acceleration = (float) table.Rows[0][9];
                MaxBackSpeed = (float) table.Rows[0][10];
                BackAcceleration =
                    (float) table.Rows[0][11];
                Cooldown = (float) table.Rows[0][12];
                Log.Information($"{Name.ToUpper()} initialized");

                Initialized = true;
                
                if (!Server.IsOnline)
                {
                    CheckForInitialize();
                }
                
                Thread.Sleep(3600000);
            }
        }

        private void CheckForInitialize()
        {
            foreach (var tank in Server.Tanks)
            {
                if (!tank.Initialized)
                {
                    return;
                }
            }
            Server.BeginListenConnections();
        }
    }
}