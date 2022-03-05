using System;

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
                
                SendTCPDataToAll(player.Id, packet);
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
        #endregion
    }
}