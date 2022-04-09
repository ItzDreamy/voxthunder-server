using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Serilog;

namespace VoxelTanksServer
{
    public static class Server
    {
        public static bool IsOnline = false;
        
        public enum Timers
        {
            Preparative,
            General
        }
        
        public static int OnlinePlayers 
        {   
            //Возвращает кол-во онлайн игроков на сервере
            get
            {
                return Clients.Values.ToList().FindAll(client => client.Tcp.Socket != null).Count;
            }
        }

        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static readonly Dictionary<int, Client> Clients = new();

        public static readonly List<Room?> Rooms = new();

        //Инициализация карт
        public static readonly List<Map> Maps = new ()
        {
            new Map("Dreamberg", new List<SpawnPoint>
            {
                new(new Vector3(9f, 0, -50)),
                new(new Vector3(3.5f, 0, -50)),
                new(new Vector3(-3.5f, 0, -50))
            }, new List<SpawnPoint>
            {
                new(new Vector3(11, 0, 45), new Quaternion(0, 180, 0, 0)),
                new(new Vector3(3, 0, 45), new Quaternion(0, 180, 0, 0)),
                new(new Vector3(-5, 0, 45), new Quaternion(0, 180, 0, 0))
            })
        };

        
        // Инициализация танков
        public static List<Tank> Tanks = new()
        {
            new Tank("Raider"),
            new Tank("Mamont")
        };

        public delegate void PacketHandler(int fromClient, Packet packet);

        public static Dictionary<int, PacketHandler> PacketHandlers;

        private static TcpListener _tcpListener;

        /// <summary>
        /// Запуск сервера
        /// </summary>
        /// <param name="maxPlayers">Максимальное кол-во игроков на сервере</param>
        /// <param name="port">Порт сервера, с помощью которого клиенты подключаются</param>
        public static void Start(int maxPlayers, int port)
        {
            try
            {
                MaxPlayers = maxPlayers;
                Port = port;

                Log.Information("Starting server...");
                InitializeServerData();
                _tcpListener = new TcpListener(IPAddress.Any, Port);

                Log.Information($"Server started on {Port}");
                Log.Information($"Max players: {MaxPlayers}");
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        /// <summary>
        /// Начать слушать подключения клиентов и подключать их
        /// </summary>
        public static void BeginListenConnections()
        {
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback), null);
            IsOnline = true;
        }

        /// <summary>
        /// Отклик на подключение клиента
        /// </summary>
        /// <param name="result"></param>
        private static void TcpConnectCallback(IAsyncResult result)
        {
            TcpClient? client = _tcpListener.EndAcceptTcpClient(result);

            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback), null);

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

        /// <summary>
        /// Инициализация серверных данных и обработчика пакетов
        /// </summary>
        private static void InitializeServerData()
        {
            //Добавление всех клиентов в словарь для дальнейшего использования
            for (var i = 1; i <= MaxPlayers; i++)
            {
                Clients.Add(i, new Client(i));
            }

            //Инициализация обработчика пакетов, приходящих от клиента
            PacketHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int) ClientPackets.WelcomeReceived, PacketsHandler.WelcomePacketReceived},
                {(int) ClientPackets.ReadyToSpawn, PacketsHandler.ReadyToSpawnReceived},
                {(int) ClientPackets.SelectTank, PacketsHandler.ChangeTank},
                {(int) ClientPackets.PlayerPositionAndRotation, PacketsHandler.SetPlayerPosition},
                {(int) ClientPackets.PlayerInput, PacketsHandler.GetPlayerInput},
                {(int) ClientPackets.RotateTurret, PacketsHandler.RotateTurret},
                {(int) ClientPackets.TryLogin, PacketsHandler.TryLogin},
                {(int) ClientPackets.TakeDamage, PacketsHandler.TakeDamage},
                {(int) ClientPackets.InstantiateObject, PacketsHandler.InstantiateObject},
                {(int) ClientPackets.ShootBullet, PacketsHandler.ShootBullet},
                {(int) ClientPackets.JoinRoom, PacketsHandler.JoinOrCreateRoom},
                {(int) ClientPackets.LeaveRoom, PacketsHandler.LeaveRoom},
                {(int) ClientPackets.CheckAbleToReconnect, PacketsHandler.CheckAbleToReconnect},
                {(int) ClientPackets.ReconnectRequest, PacketsHandler.Reconnect},
                {(int) ClientPackets.CancelReconnect, PacketsHandler.CancelReconnect},
                {(int) ClientPackets.RequestPlayersStats, PacketsHandler.RequestPlayersStats},
                {(int) ClientPackets.LeaveToLobby, PacketsHandler.LeaveToLobby}
            };
            Log.Information("Packets initialized");
        }
    }
}