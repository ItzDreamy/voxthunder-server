using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Serilog;

namespace VoxelTanksServer
{
    public static class Server
    {
        public static bool IsOnline = false;
        
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
        public static Dictionary<int, Client> Clients = new();

        public static List<Room?> Rooms = new();

        //Инициализация карт
        public static List<Map> Maps = new ()
        {
            new Map("FirstMap", new List<SpawnPoint>
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
            new Tank("raider"),
            new Tank("mamont")
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
                {(int) ClientPackets.LeaveRoom, ServerHandle.LeaveRoom},
                {(int) ClientPackets.CheckAbleToReconnect, ServerHandle.CheckAbleToReconnect},
                {(int) ClientPackets.ReconnectRequest, ServerHandle.Reconnect},
                {(int) ClientPackets.CancelReconnect, ServerHandle.CancelReconnect},
            };
            Log.Information("Packets initialized");
        }
    }
}