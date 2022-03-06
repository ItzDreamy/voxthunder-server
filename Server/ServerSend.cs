using System;
using System.Numerics;
using Serilog;

namespace VoxelTanksServer
{
    public static class ServerSend
    {
        private static void SendTCPData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Tcp.SendData(packet);
        }

        private static void SendTCPDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                Server.Clients[i].Tcp.SendData(packet);
            }
        }
        
        private static void SendTCPDataToAll(int exceptClient, Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                if(i != exceptClient)
                    Server.Clients[i].Tcp.SendData(packet);
            }
        }

        #region Packets

        public static void Welcome(int toClient, string message)
        {
            using (Packet packet = new Packet((int)ServerPackets.Welcome))
            {
                packet.Write(message);
                packet.Write(toClient);

                SendTCPData(toClient, packet);
            }
        }
        
        public static void SpawnPlayer(int toClient, Player player)
        {
            using (Packet packet = new Packet((int)ServerPackets.SpawnPlayer))
            {
                packet.Write(player.Id);
                packet.Write(player.Username);
                packet.Write(player.Position);
                packet.Write(player.Rotation);  
                packet.Write(player.TankName);
                packet.Write(player.Cooldown);
                packet.Write(player.MaxHealth);
                
                SendTCPData(toClient, packet);
            }
        }

        public static void MovePlayer(Player player)
        {
            using (Packet packet = new Packet((int) ServerPackets.PlayerMovement))
            {
                packet.Write(player.Id);
                packet.Write(player.Position);
                packet.Write(player.Rotation);
                packet.Write(player.BarrelRotation);
                
                SendTCPDataToAll(packet);
            }
        }
        
        public static void RotateTurret(Player player)
        {
            using (Packet packet = new Packet((int) ServerPackets.RotateTurret))
            {
                packet.Write(player.Id);
                packet.Write(player.TurretRotation);
                
                SendTCPDataToAll(player.Id, packet);
            }
        }

        public static void LoginResult(int toClient, bool result)
        {
            using (Packet packet = new Packet((int) ServerPackets.LoginResult))
            {
                packet.Write(result);

                SendTCPData(toClient, packet);
            }
        }

        public static void InstantiateObject(string name, Vector3 position, Quaternion rotation, int fromClient)
        {
            using (Packet packet = new Packet((int) ServerPackets.InstantiateObject))
            {
                packet.Write(name);
                packet.Write(position);
                packet.Write(rotation);
                packet.Write(fromClient);
                
                SendTCPDataToAll(packet);
            }
        }
        
        public static void PlayerDisconnected(int playerId)
        {
            using (Packet packet = new Packet((int) ServerPackets.PlayerDisconnected))
            {
                packet.Write(playerId);
                
                SendTCPDataToAll(packet);
            }
        }

        public static void TakeDamage(int playerId, int maxHealth, int currentHealth)
        {
            using (Packet packet = new Packet((int) ServerPackets.TakeDamage))
            {
                packet.Write(playerId);
                packet.Write(maxHealth);
                packet.Write(currentHealth);
                
                SendTCPData(playerId, packet);
            }
        }

        public static void PlayerDead(int playerId)
        {
            using (Packet packet = new Packet((int) ServerPackets.PlayerDead))
            {
                packet.Write(playerId);
                
                SendTCPDataToAll(packet);
            }
        }
        #endregion
    }
}