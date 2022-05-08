using Newtonsoft.Json.Linq;
using Serilog;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;
using VoxelTanksServer.Library.Config;
using VoxelTanksServer.Protocol;
using VoxelTanksServer.Protocol.API;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer;

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
        {"smp_room", Commands.SetMaxPlayersInRoom},
        {"clear", Console.Clear},
        {
            "g_time", () =>
            {
                Console.Write("General time: ");
                Server.Config.GeneralTime = int.Parse(Console.ReadLine());
            }
        },
        {
            "p_time", () =>
            {
                Console.Write("Preparative time: ");
                Server.Config.PreparativeTime = int.Parse(Console.ReadLine());
            }
        }
    };

    public static void Main(string[] args)
    {
        JArray obj = JArray.Parse(File.ReadAllText("PlayersData/questsData.json"));
        foreach (var something in obj)
        {
            Console.WriteLine(something["nickname"]);
        }
        
        Console.Title = "Server";
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Hour)
                .CreateLogger();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var config = deserializer.Deserialize<Config>(File.ReadAllText("Library/Config/config.yml"));

            _isRunning = true;

            Thread mainThread = new(MainThread);
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