using System;
using System.Threading;

namespace VoxelTanksServer
{
    public static class Program
    {
        private static bool IsRunning;
        public static void Main(string[] args)
        {
            Console.Title = "VoxelTanksServer";
            
            IsRunning = true;
            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();
            
            Server.Start(100, 25565);
            
        }

        private static void MainThread()
        {
            Console.WriteLine($"[INFO] Main thread started. Tickrate: {Constants.Tickrate}");
            DateTime nextLoop = DateTime.Now;

            while (IsRunning)
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