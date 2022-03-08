using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
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

                Server.Start(config.MaxPlayers, config.Port);
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
        public int MaxPlayers;
        public int Port;
    }
}