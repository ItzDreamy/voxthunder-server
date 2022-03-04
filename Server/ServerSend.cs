namespace VoxelTanksServer
{
    public class ServerSend
    {
        public static void Welcome(int toClient, string message)
        {
            using (Packet packet = new Packet((int)ServerPackets.Welcome))
            {
                packet.Write(message);
                packet.Write(toClient);

                SendTCPData(toClient, packet);
            }
        }

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
    }
}