using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace VoxelTanksServer
{
    public static class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();

        public delegate void PacketHandler(int fromClient, Packet packet);

        public static Dictionary<int, PacketHandler> PacketHandlers;

        private static TcpListener _tcpListener;

        public static void Start(int maxPlayers, int port)
        {
            MaxPlayers = maxPlayers;
            Port = port;

            Console.WriteLine("[INFO] Starting server...");
            InitializeServerData();
            _tcpListener = new TcpListener(IPAddress.Any, Port);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"[INFO] Server started on {Port}");
        }

        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = _tcpListener.EndAcceptTcpClient(result);
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"[INFO] Trying to connect {client.Client.RemoteEndPoint}");
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (Clients[i].Tcp.Socket == null)
                {
                    Clients[i].Tcp.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"[INFO] {client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                Clients.Add(i, new Client(i));
            }

            PacketHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int) ClientPackets.WelcomeReceived, ServerHandle.WelcomePacketReceived},
                {(int) ClientPackets.ReadyToSpawn, ServerHandle.ReadyToSpawnReceived},
                {(int) ClientPackets.SelectTank, ServerHandle.ChangeTank},
                {(int) ClientPackets.PlayerMovement, ServerHandle.PlayerMovement},
                {(int) ClientPackets.RotateTurret, ServerHandle.RotateTurret},
                {(int) ClientPackets.TryLogin, ServerHandle.TryLogin}
            };
            Console.WriteLine("[INFO] Packets initialized");
        }
    }
}