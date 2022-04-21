using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace VoxelTanksServer.API
{
    public static class ApiServer
    {
        private static int _maxConnections;
        private static int _port;
        public static readonly Dictionary<int, ApiClient> Clients = new();
        
        public delegate void PacketHandler(int fromClient, Packet packet);

        public static Dictionary<int, PacketHandler>? PacketHandlers;

        private static TcpListener? _tcpListener;

        public static void Start(Config config)
        {
            try
            {
                _maxConnections = config.ApiMaxConnections;
                _port = config.ApiPort;

                Log.Information("Starting api...");
                InitializeServerData();
                _tcpListener = new TcpListener(IPAddress.Any, _port);
                _tcpListener.Start();
                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback), null);

                Log.Information($"Api started on {_port}");
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
            for (int i = 1; i <= _maxConnections; i++)
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
            for (int i = 1; i <= _maxConnections; i++)
            {
                Clients.Add(i, new ApiClient(i));
            }

            PacketHandlers = new Dictionary<int, PacketHandler>
            {
                {(int) ClientApiPackets.GetPlayersCount, ApiHandle.GetPlayersCount},
                {(int) ClientApiPackets.GetServerState, ApiHandle.GetServerState}
            };
        }
    }
}