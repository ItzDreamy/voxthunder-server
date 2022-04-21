using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Serilog;
using VoxelTanksServer.API;
using VoxelTanksServer.GameCore;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer
{
    public static class Program
    {
        private static bool _isRunning;

        private static readonly Dictionary<string, Action> ServerCommands = new()
        {
            {"online", Commands.ShowOnline},
            {"kick", Commands.KickPlayer},
            {"ban", Commands.BanPlayer},
            {"stop", Commands.StopServer},
            {"info", Commands.ShowInfo},
            {"players", Commands.ShowPlayerList},
            {"help", Commands.ShowCommandList},
            {"smp_room", Commands.SetMaxPlayersInRoom}
        };

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
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var config = deserializer.Deserialize<Config>(File.ReadAllText("Configs/config.yml"));

                _isRunning = true;

                //Запуск основного потока сервера
                Thread mainThread = new(MainThread);

                //Запуск потока для выполнения консольных команд
                Thread commandsThread = new(() =>
                {
                    while (_isRunning)
                    {
                        var command = Console.ReadLine()?.ToLower();
                        if (command != null && ServerCommands.ContainsKey(command))
                        {
                            ServerCommands[command]();
                        }
                        else
                        {
                            Console.WriteLine("Command doesnt exists");
                        }
                    }
                });
                commandsThread.Start();
                mainThread.Start();

                //Запуск сервера + апи
                Server.Start(config);
                ApiServer.Start(config);

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

                    if (nextLoop > DateTime.Now && nextLoop - DateTime.Now >= TimeSpan.Zero)
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
        public int AfkTime;
        public int MaxPlayers;
        public int ServerPort;
        public int ApiPort;
        public int ApiMaxConnections;
        public int MaxPlayersInRoom;
    }
}