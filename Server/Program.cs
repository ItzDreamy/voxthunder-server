using Serilog;
using VoxelTanksServer.API;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer
{
    public static class Program
    {
        private static bool _isRunning;

        public static void Main(string[] args)
        {
            Console.Title = "VoxelTanksServer";

            try
            {
                //Инициализация логгера
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                //Чтение конфига
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var config = deserializer.Deserialize<Config>(File.ReadAllText("Configs/config.yml"));

                _isRunning = true;

                //Запуск основного потока сервера
                Thread mainThread = new(new ThreadStart(MainThread));

                //Запуск потока для выполнения консольных команд
                Thread commandsThread = new(() =>
                {
                    while (_isRunning)
                    {
                        string? command = Console.ReadLine();
                        
                        switch (command)
                        {
                            case "online":
                                Console.WriteLine($"Current online: {Server.OnlinePlayers} / {Server.MaxPlayers}");
                                break;
                            case "kick":
                                Console.WriteLine("Введите никнейм который нужно кикнуть");
                                var client = Server.Clients.Values.ToList().Find(c => c.Username.ToLower() == Console.ReadLine().ToLower());

                                if (client == null) return;
                                client.Disconnect("Решение администратора");
                                break;
                            case "ban-nickname":
                                Console.WriteLine("Введите никнейм который нужно заблокировать");
                                string input = Console.ReadLine().ToLower();
                                var client1 = Server.Clients.Values.ToList().Find(c => c.Username.ToLower() == input);

                                if (client1 == null) return;
                                client1.Disconnect("Блокировка пользователя");

                                break;
                        }

                    }
                });
                commandsThread.Start();
                mainThread.Start();
                
                //Запуск сервера + апи
                Server.Start(config.MaxPlayers, config.ServerPort);
                ApiServer.Start(config.ApiMaxConnections, config.ApiPort);

                Log.Information($"Client version: {config.ClientVersion}");
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                Console.ReadLine();
            }
        }


        /// <summary>
        /// Обновление сервера
        /// </summary>
        private static void MainThread()
        {
            Log.Information($"Main thread started. Tickrate: {Constants.Tickrate}");
            DateTime nextLoop = DateTime.Now;

            while (_isRunning)
            {
                while (nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    nextLoop = nextLoop.AddMilliseconds(Constants.MsPerTick);

                    if (nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }

    public class Config
    {
        public string ClientVersion;
        public int MaxPlayers;
        public int ServerPort;
        public int ApiPort;
        public int ApiMaxConnections;
    }
}