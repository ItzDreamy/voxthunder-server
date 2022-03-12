﻿using System;
using System.Numerics;
using Serilog;
using Serilog.Core;

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
            string? tankName = packet.ReadString();
            Server.Clients[fromClient].SelectedTank = tankName;
        }

        public static void PlayerMovement(int fromClient, Packet packet)
        {
            Vector3 playerPosition = packet.ReadVector3();
            Quaternion playerRotation = packet.ReadQuaternion();
            Quaternion barrelRotation = packet.ReadQuaternion();
            bool isForward = packet.ReadBool();
            float speed = packet.ReadFloat();

            Player? player = Server.Clients[fromClient].Player;
            if (player != null)
            {
                player.Move(playerPosition, playerRotation, barrelRotation, speed, isForward);
            }
        }

        public static void RotateTurret(int fromClient, Packet packet)
        {
            Quaternion turretRotation = packet.ReadQuaternion();
            Player? player = Server.Clients[fromClient].Player;
            if (player != null)
            {
                player.RotateTurret(turretRotation);
            }
        }

        public static void TryLogin(int fromClient, Packet packet)
        {
            string? username = packet.ReadString();
            string? password = packet.ReadString();
            bool correctData = AuthorizationHandler.ClientAuthRequest(username, password,
                Server.Clients[fromClient].Tcp.Socket.Client.RemoteEndPoint?.ToString(), fromClient,
                out string? message);
            if (correctData)
            {
                Server.Clients[fromClient].Username = username;
            }

            ServerSend.LoginResult(fromClient, correctData, message);
        }

        public static void InstantiateObject(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect();
            }

            string? name = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            ServerSend.InstantiateObject(name, position, rotation, fromClient,
                Server.Clients[fromClient].ConnectedRoom);
        }

        public static void ShootBullet(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect();
            }

            string? name = packet.ReadString();
            string? particlePrefab = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            Player? player = Server.Clients[fromClient].Player;
            player?.Shoot(name, particlePrefab, position, rotation);
        }

        public static void TakeDamage(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect();
            }

            int enemyId = packet.ReadInt();
            Player? enemy = Server.Clients[enemyId].Player;
            Player? hitPlayer = Server.Clients[fromClient].Player;
            
            if (enemy != null && hitPlayer != null && enemy.Team.ID != hitPlayer.Team.ID)
            {
                int damage = enemy.Damage;
                float randomCoof = new Random().Next(-20, 20) * ((float) damage / 100);
                int calculatedDamage = damage + (int) randomCoof;

                if (calculatedDamage == 0)
                {
                    calculatedDamage = damage;
                }
                else if (calculatedDamage > hitPlayer.Health)
                {
                    calculatedDamage = hitPlayer.Health;
                }

                hitPlayer.TakeDamage(calculatedDamage, enemy);
                enemy.TotalDamage += calculatedDamage;
                if (hitPlayer.Health > 0)
                {
                    ServerSend.ShowDamage(enemy.Id, calculatedDamage, hitPlayer.Position);
                }
            }
        }

        public static void JoinOrCreateRoom(int fromClient, Packet packet)
        {
            Client packetSender = Server.Clients[fromClient];
            if (!packetSender.IsAuth)
            {
                packetSender.Disconnect();
            }

            if (Server.Rooms.Count > 0)
            {
                foreach (var room in Server.Rooms)
                {
                    if (room.IsOpen)
                    {
                        Client client = Server.Clients[fromClient];
                        if (client == null) return;

                        room.Players[fromClient] = client;
                        client.ConnectedRoom = room;
                        if (room.PlayersCount == room.MaxPlayers)
                        {
                            room.IsOpen = false;
                            room.BalanceTeams();
                        }

                        return;
                    }
                }
            }

            Room? newRoom = new Room(2)
            {
                Players =
                {
                    [fromClient] = Server.Clients[fromClient]
                }
            };
            Server.Clients[fromClient].ConnectedRoom = newRoom;

            if (newRoom.PlayersCount == newRoom.MaxPlayers)
            {
                newRoom.IsOpen = false;
                newRoom.BalanceTeams();
            }
        }

        public static void LeaveRoom(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect();
            }

            Room? playerRoom = Server.Clients[fromClient].ConnectedRoom;
            if (!playerRoom.IsOpen) return;
            Server.Clients[fromClient].LeaveRoom();
        }

        public static void CheckAbleToReconnect(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect();
            }
            
            foreach (var room in Server.Rooms)
            {
                foreach (var cachedPlayer in room.CachedPlayers)
                {
                    if (cachedPlayer?.Username.ToLower() == Server.Clients[fromClient].Username?.ToLower() && cachedPlayer.IsAlive)
                    {
                        ServerSend.AbleToReconnect(fromClient);
                        Log.Information($"{Server.Clients[fromClient].Username} can reconnect to battle");
                    }
                }
            }
        }


        public static void Reconnect(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect();
            }

            foreach (var room in Server.Rooms)
            {
                foreach (var cachedPlayer in room?.CachedPlayers!)
                {
                    if (cachedPlayer?.Username?.ToLower() == Server.Clients[fromClient].Username?.ToLower())
                    {
                        room.Players[fromClient] = client;
                        client.ConnectedRoom = room;
                        client.Team = cachedPlayer.Team;
                        client.Team.Players.Add(client);
                        ServerSend.LoadScene(fromClient, room.Map.Name);
                        client.Player = new Player(cachedPlayer, fromClient);
                        return;
                    }
                }
            }
        }

        public static void CancelReconnect(int fromClient, Packet packet)
        {
            Client client = Server.Clients[fromClient];
            if (!client.IsAuth)
            {
                client.Disconnect();
            }

            foreach (var room in Server.Rooms)
            {
                foreach (var cachedPlayer in room.CachedPlayers)
                {
                    if (cachedPlayer?.Username == Server.Clients[fromClient].Username)
                    {
                        Log.Information($"{cachedPlayer.Username} canceled reconnect");
                        room.CachedPlayers[room.CachedPlayers.IndexOf(cachedPlayer)] = null;
                        return;
                    }
                }
            }
        }
    }
}