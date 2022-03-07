using System;
using System.Numerics;
using Serilog;

namespace VoxelTanksServer
{
    public static class ServerHandle
    {
        public static void WelcomePacketReceived(int fromClient, Packet packet)
        {
            int clientIdCheck = packet.ReadInt();
            Log.Information(
                $"{Server.Clients[fromClient].Tcp.Socket.Client.RemoteEndPoint} connected successfully with ID {fromClient}");

            if (fromClient != clientIdCheck)
            {
                Log.Warning(
                    $"Player (ID: {fromClient}) has the wrong client ID ({clientIdCheck})");
            }
        }
        
        public static void ReadyToSpawnReceived(int fromClient, Packet packet)
        {
            Server.Clients[fromClient]
                .SendIntoGame(Server.Clients[fromClient].Username, Server.Clients[fromClient].SelectedTank);
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
            bool isForward = packet.ReadBool();
            float speed = packet.ReadFloat();

            Player player = Server.Clients[fromClient].Player;
            if (player != null)
            {
                player.Move(playerPosition, playerRotation, barrelRotation, speed, isForward);
            }
        }

        public static void RotateTurret(int fromClient, Packet packet)
        {
            Quaternion turretRotation = packet.ReadQuaternion();
            Player player = Server.Clients[fromClient].Player;
            if (player != null)
            {
                player.RotateTurret(turretRotation);
            }
        }

        public static void TryLogin(int fromClient, Packet packet)
        {
            string username = packet.ReadString();
            string password = packet.ReadString();
            bool correctData = AuthorizationHandler.ClientAuthRequest(username, password,
                Server.Clients[fromClient].Tcp.Socket.Client.RemoteEndPoint.ToString(), out string message);
            if (correctData)
            {
                Server.Clients[fromClient].Username = username;
            }
            ServerSend.LoginResult(fromClient, correctData, message);
        }

        public static void InstantiateObject(int fromClient, Packet packet)
        {
            string name = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            ServerSend.InstantiateObject(name, position, rotation, fromClient, Server.Clients[fromClient].ConnectedRoom);
        }

        public static void ShootBullet(int fromClient, Packet packet)
        {
            string name = packet.ReadString();
            string particlePrefab = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            Player player = Server.Clients[fromClient].Player;
            player.Shoot(name, particlePrefab, position, rotation);
        }

        public static void TakeDamage(int fromClient, Packet packet)
        {
            int enemyId = packet.ReadInt();
            Player enemy = Server.Clients[enemyId].Player;
            if (enemy != null)
            {
                int damage = enemy.Damage;
                float randomCoof = new Random().Next(-20, 20) * ((float) damage / 100);
                int calculatedDamage = damage + (int) randomCoof;

                if (calculatedDamage == 0)
                {
                    calculatedDamage = damage;
                }

                Server.Clients[fromClient].Player.TakeDamage(calculatedDamage);
                enemy.TotalDamage += calculatedDamage;   
            }
        }

        public static void JoinOrCreateRoom(int fromClient, Packet packet)
        {
            if (Server.Rooms.Count > 0)
            {
                foreach (var room in Server.Rooms)
                {
                    if (room.IsOpen)
                    {
                        room.Players[fromClient] = Server.Clients[fromClient];
                        Server.Clients[fromClient].ConnectedRoom = room;
                        Log.Information($"Client {fromClient} connected to room");
                        Log.Information($"Current players in room {Server.Rooms.IndexOf(room)} : {room.PlayersCount} / {room.MaxPlayers}");
                        if (room.PlayersCount == room.MaxPlayers)
                        {
                            room.IsOpen = false;
                            ServerSend.LoadGame(room);
                        }
                        return;
                    }
                }
            }
            Room newRoom = new Room(2);
            Log.Information($"Client {fromClient} created the room {Server.Rooms.IndexOf(newRoom)}");
            
            newRoom.Players[fromClient] = Server.Clients[fromClient];
            Server.Clients[fromClient].ConnectedRoom = newRoom;
        }

        public static void LeaveRoom(int fromClient, Packet packet)
        {
            Room playerRoom = Server.Clients[fromClient].ConnectedRoom;
            Server.Clients[fromClient].LeftRoom();
        }
    }
}