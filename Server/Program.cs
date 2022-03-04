using System;

namespace VoxelTanksServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "VoxelTanksServer";
            
            Server.Start(100, 25565);
            
            Console.ReadLine();
        }
    }
}