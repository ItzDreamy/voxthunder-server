using System;

namespace VoxelTanksServer
{
    public class ServerHandle
    {
        public static void WelcomePacketReceived(int fromClient, Packet packet)
        {
            int clientIdCheck = packet.ReadInt();
            string username = packet.ReadString();
            Server.Clients[fromClient].Username = username;
            Console.WriteLine($"[INFO] {Server.Clients[fromClient].Tcp.Socket.Client.RemoteEndPoint} connected successfully as {username} with ID {fromClient}");

            if (fromClient != clientIdCheck)
            {
                Console.WriteLine($"[INFO] Player \"{username}\" (ID: {fromClient}) has the wrong client ID ({clientIdCheck})");
            }
        }

        public static void ReadyToSpawnReceived(int fromClient, Packet packet)
        {
            Server.Clients[fromClient].SendIntoGame(Server.Clients[fromClient].Username);
        }
    }
}