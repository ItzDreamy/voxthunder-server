using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace VoxelTanksServer.API
{
    public static class ApiServer
    {
        public static int MaxConnections { get; private set; }
        public static int Port { get; private set; }
        public static readonly Dictionary<int, ApiClient> Clients = new();
        
        public delegate void PacketHandler(int fromClient, Packet packet);

        public static Dictionary<int, PacketHandler>? PacketHandlers;

        private static TcpListener? _tcpListener;

        public static void Start(int maxPlayers, int port)
        {
            try
            {
                MaxConnections = maxPlayers;
                Port = port;

                Log.Information("Starting api...");
                InitializeServerData();
                _tcpListener = new TcpListener(IPAddress.Any, Port);
                _tcpListener.Start();
                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback), null);

                Log.Information($"Api started on {Port}");
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private static void TcpConnectCallback(IAsyncResult result)
        {
            TcpClient? client = _tcpListener?.EndAcceptTcpClient(result);
            _tcpListener?.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback), null);
            for (int i = 1; i <= MaxConnections; i++)
            {
                if (Clients[i].Tcp.Socket == null)
                {
                    Clients[i].Tcp.Connect(client);
                    return;
                }
            }

            Log.Information($"{client?.Client.RemoteEndPoint} failed to connect: Api server full!");
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxConnections; i++)
            {
                Clients.Add(i, new ApiClient(i));
            }

            PacketHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int) ClientApiPackets.GetPlayersCount, ApiHandle.GetPlayersCount},
                {(int) ClientApiPackets.GetServerState, ApiHandle.GetServerState}
            };
        }
    }
}