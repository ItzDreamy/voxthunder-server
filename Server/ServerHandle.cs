using System;
using System.Numerics;

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
            Server.Clients[fromClient].SendIntoGame(Server.Clients[fromClient].Username, Server.Clients[fromClient].SelectedTank);
        }

        public static void ChangeTank(int fromClient, Packet packet)
        {
            string tankName = packet.ReadString();
            Server.Clients[fromClient].SelectedTank = tankName;
        }

        public static void PlayerMovement(int fromClient, Packet packet)
        {
            Vector3 playerPosition = packet.ReadVector3();
            Quaternion playerRotation = packet.ReadQuaternion();
            Quaternion barrelRotation = packet.ReadQuaternion();
            
            Player player = Server.Clients[fromClient].Player;
            
            player.Move(playerPosition, playerRotation, barrelRotation);
        }

        public static void RotateTurret(int fromClient, Packet packet)
        {
            Quaternion turretRotation = packet.ReadQuaternion();
            Player player = Server.Clients[fromClient].Player;
            player.RotateTurret(turretRotation);
        }
    }
}