using System.Data;
using MySql.Data.MySqlClient;
using Serilog;

namespace VoxelTanksServer
{
    /// <summary>
    /// Класс для хранения данных о танке
    /// </summary>
    public class Tank
    {
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

        private bool _initialized;

        public Tank(string name)
        {
            Name = name;
            //Запуск потока для инициализации танка
            Thread databaseThread = new(GetStats);
            databaseThread.Start();
        }

        /// <summary>
        /// Получать данные о танке каждый час
        /// </summary>
        private void GetStats()
        {
            while (true)
            {
                //Запрос к БД
                var db = new Database();
                MySqlCommand myCommand =
                    new(
                        $"SELECT * FROM `tanksstats` WHERE `tankname` = '{Name.ToLower()}'",
                        db.GetConnection());
                MySqlDataAdapter adapter = new();
                DataTable table = new();
                adapter.SelectCommand = myCommand;
                adapter.Fill(table);

                //Установка значений
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
                _initialized = true;
                
                if (!Server.IsOnline)
                {
                    CheckForInitialize();
                }
                
                Thread.Sleep(3600000);
            }
        }

        /// <summary>
        /// Запускать слушатель клиентов, если танки проинциализированы
        /// </summary>
        private void CheckForInitialize()
        {
            foreach (var tank in Server.Tanks)
            {
                if (!tank._initialized)
                {
                    return;
                }
            }
            Server.BeginListenConnections();
        }
    }
}