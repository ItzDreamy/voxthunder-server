using System.Text;
using MySql.Data.MySqlClient;
using Serilog;
using VoxelTanksServer.Database;
using VoxelTanksServer.Discord;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;
using VoxelTanksServer.Library.Config;
using VoxelTanksServer.Protocol;
using VoxelTanksServer.Protocol.API;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer;

public static class Program {
    private static bool _isRunning;

    private static readonly Dictionary<string, Action> ServerCommands = new() {
        {"online", Commands.ShowOnline},
        {"kick", Commands.KickPlayer},
        {"ban", Commands.BanPlayer},
        {"stop", Commands.StopServer},
        {"info", Commands.ShowInfo},
        {"players", Commands.ShowPlayerList},
        {"help", Commands.ShowCommandList},
        {"smp_room", Commands.SetMaxPlayersInRoom},
        {"clear", Console.Clear}, {
            "g_time", () => {
                Console.Write("General time: ");
                Server.Config.GeneralTime = int.Parse(Console.ReadLine());
            }
        }, {
            "p_time", () => {
                Console.Write("Preparative time: ");
                Server.Config.PreparativeTime = int.Parse(Console.ReadLine());
            }
        }
    };

    public static void Main(string[] args) {
        Console.Title = "Server";
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try {
            IDatabaseService databaseService = new DatabaseService();
            SetupLogger();
            SetupServer(databaseService);

            new DiscordStartUp().MainAsync();
        }
        catch (Exception e) {
            Log.Error(e.ToString());
            Console.ReadLine();
        }
    }

    private static void SetupServer(IDatabaseService databaseService) {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var serverConfig = deserializer.Deserialize<Config>(File.ReadAllText("Library/Config/config.yml"));

        _isRunning = true;

        Thread mainThread = new(MainThread);
        StartCommandThread();
        mainThread.Start();

        Server.Start(serverConfig, databaseService);
        ApiServer.Start(serverConfig);

        Log.Information($"Client version: {serverConfig.ClientVersion}");
    }

    private static void StartCommandThread() {
        Thread commandsThread = new(() => {
            while (_isRunning) {
                var command = Console.ReadLine()?.ToLower();
                if (command != null && ServerCommands.ContainsKey(command))
                    ServerCommands[command]();
                else
                    Console.WriteLine("Command doesnt exists");
            }
        });
        commandsThread.Start();
    }

    private static void SetupLogger() {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Hour)
            .CreateLogger();
    }

    private static void MainThread() {
        Log.Information($"Main thread started. Tickrate: {Constants.Tickrate}");
        var nextLoop = DateTime.Now;

        while (_isRunning) {
            while (nextLoop < DateTime.Now) {
                GameLogic.Update();

                nextLoop = nextLoop.AddMilliseconds(Constants.MsPerTick);

                if (nextLoop > DateTime.Now && nextLoop - DateTime.Now >= TimeSpan.Zero)
                    Thread.Sleep(nextLoop - DateTime.Now);
            }
        }
    }
}