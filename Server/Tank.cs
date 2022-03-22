using System.Data;
using MySql.Data.MySqlClient;
using Serilog;

namespace VoxelTanksServer
{
    public class Tank
    {
        public bool Initialized { get; private set; }

        public string Name { get; private set; }
        public int Damage { get; private set; }
        public int MaxHealth { get; private set; }
        public float TowerRotateSpeed { get; private set; }
        public float TankRotateSpeed { get; private set; }
        public float AngleUp { get; private set; }
        public float AngleDown { get; private set; }
        public float MaxSpeed { get; private set; }
        public float Acceleration { get; private set; }
        public float MaxBackSpeed { get; private set; }
        public float BackAcceleration { get; private set; }
        public float Cooldown { get; private set; }

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
                Log.Information($"Tank {Name.ToUpper()} initialized");

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