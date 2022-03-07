using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace VoxelTanksServer
{
    public static class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();

        public static List<Room> Rooms = new List<Room>();

        public delegate void PacketHandler(int fromClient, Packet packet);

        public static Dictionary<int, PacketHandler> PacketHandlers;

        private static TcpListener _tcpListener;

        public static void Start(int maxPlayers, int port)
        {
            MaxPlayers = maxPlayers;
            Port = port;

            Log.Information("Starting server...");
            InitializeServerData();
            _tcpListener = new TcpListener(IPAddress.Any, Port);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Log.Information($"Server started on {Port}");
        }

        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = _tcpListener.EndAcceptTcpClient(result);
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Log.Information($"Trying to connect {client.Client.RemoteEndPoint}");
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (Clients[i].Tcp.Socket == null)
                {
                    Clients[i].Tcp.Connect(client);
                    return;
                }
            }

            Log.Information($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
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
                {(int) ClientPackets.TryLogin, ServerHandle.TryLogin},
                {(int) ClientPackets.TakeDamage, ServerHandle.TakeDamage},
                {(int) ClientPackets.InstantiateObject, ServerHandle.InstantiateObject},
                {(int) ClientPackets.ShootBullet, ServerHandle.ShootBullet},
                {(int) ClientPackets.JoinRoom, ServerHandle.JoinOrCreateRoom},
                {(int) ClientPackets.LeaveRoom, ServerHandle.LeaveRoom}
            };
            Log.Information("Packets initialized");
        }
    }
}