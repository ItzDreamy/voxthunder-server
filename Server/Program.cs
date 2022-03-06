using System;
using System.Threading;
using Serilog;

namespace VoxelTanksServer
{
    public static class Program
    {
        private static bool _isRunning;
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            
            Console.Title = "VoxelTanksServer";
            
            _isRunning = true;
            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();
            
            Server.Start(100, 25565);
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
}