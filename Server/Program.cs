using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Serilog.Core;
using VoxelTanksServer.API;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer
{
    public static class Program
    {
        private static bool _isRunning;

        public static void Main(string[] args)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var config = deserializer.Deserialize<Config>(File.ReadAllText("Configs/config.yml"));

                Console.Title = "VoxelTanksServer";

                _isRunning = true;
                Thread mainThread = new Thread(new ThreadStart(MainThread));
                Thread commandsThread = new Thread(() =>
                {
                    while (_isRunning)
                    {
                        string? command = Console.ReadLine();
                        //TODO: Some console commands
                    }
                });
                commandsThread.Start();
                mainThread.Start();

                Server.Start(config.MaxPlayers, config.ServerPort);
                ApiServer.Start(config.ApiMaxConnections, config.ApiPort);

                Log.Information($"Client version: {config.ClientVersion}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void MainThread()
        {
            Log.Information($"Main thread started. Tickrate: {Constants.Tickrate}");
            DateTime nextLoop = DateTime.Now;
            while (_isRunning)
            {
                while (nextLoop < DateTime.Now)
                {
                    // If the time for the next loop is in the past, aka it's time to execute another tick
                    GameLogic.Update(); // Execute game logic

                    nextLoop = nextLoop.AddMilliseconds(Constants.MsPerTick); // Calculate at what point in time the next tick should be executed

                    if (nextLoop > DateTime.Now)
                    {
                        // If the execution time for the next tick is in the future, aka the server is NOT running behind
                        Thread.Sleep(nextLoop - DateTime.Now); // Let the thread sleep until it's needed again.
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