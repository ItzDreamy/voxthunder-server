using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Serilog;
using VoxelTanksServer.Database;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library.Config;

namespace VoxelTanksServer.Protocol;

public static class Server {
    public delegate void PacketHandler(int fromClient, Packet packet);

    public static IDatabaseService DatabaseService { get; private set; }

    public static bool IsOnline;
    public static Config? Config;
    private static int _port;
    public static readonly Dictionary<int, Client> Clients = new();
    public static readonly List<Room?> Rooms = new();

    public static readonly List<Map> Maps = new() {
        new Map("Dreamberg", new List<SpawnPoint> {
            new(new Vector3(9f, 0, -50)),
            new(new Vector3(3.5f, 0, -50)),
            new(new Vector3(-3.5f, 0, -50))
        }, new List<SpawnPoint> {
            new(new Vector3(11, 0, 45), new Quaternion(0, 180, 0, 0)),
            new(new Vector3(3, 0, 45), new Quaternion(0, 180, 0, 0)),
            new(new Vector3(-5, 0, 45), new Quaternion(0, 180, 0, 0))
        })
    };

    public static Dictionary<int, PacketHandler>? PacketHandlers;

    private static TcpListener? _tcpListener;

    public static int OnlinePlayers {
        get { return Clients.Values.ToList().FindAll(client => client.Tcp.Socket != null).Count; }
    }

    public static int MaxPlayers { get; private set; }

    public static void Start(Config config, IDatabaseService databaseService) {
        try {
            Config = config;
            MaxPlayers = config.MaxPlayers;
            _port = config.ServerPort;
            DatabaseService = databaseService;

            Log.Information("Starting server...");
            InitializeServerData();
            _tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, _port));

            Log.Information($"Server started on {_port}");
            Log.Information($"Max players: {MaxPlayers}");

            BeginListenConnections();
        }
        catch (Exception e) {
            Log.Error(e.ToString());
        }
    }

    public static void BeginListenConnections() {
        _tcpListener?.Start();
        _tcpListener?.BeginAcceptTcpClient(TcpConnectCallback, null);
        IsOnline = true;
    }

    private static void TcpConnectCallback(IAsyncResult result) {
        var client = _tcpListener?.EndAcceptTcpClient(result);
        _tcpListener?.BeginAcceptTcpClient(TcpConnectCallback, null);
        Log.Information($"Trying to connect {client?.Client.RemoteEndPoint}");

        for (var i = 1; i <= MaxPlayers; i++)
            if (Clients[i].Tcp.Socket == null) {
                Clients[i].Tcp.Connect(client);
                return;
            }

        Log.Information($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    private static void InitializeServerData() {
        for (var i = 1; i <= MaxPlayers; i++) Clients.Add(i, new Client(i));

        PacketHandlers = new Dictionary<int, PacketHandler> {
            {(int) ClientPackets.WelcomeReceived, PacketsHandler.WelcomePacketReceived},
            {(int) ClientPackets.ReadyToSpawn, PacketsHandler.ReadyToSpawnReceived},
            {(int) ClientPackets.SelectTank, PacketsHandler.ChangeTank},
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
            {(int) ClientPackets.LeaveToLobby, PacketsHandler.LeaveToLobby},
            {(int) ClientPackets.SendMovement, PacketsHandler.GetPlayerMovement},
            {(int) ClientPackets.RequestData, PacketsHandler.HandleDataRequest},
            {(int) ClientPackets.AuthById, PacketsHandler.AuthById},
            {(int) ClientPackets.SignOut, PacketsHandler.SignOut},
            {(int) ClientPackets.ReceiveMessage, PacketsHandler.ReceiveMessage},
            {(int) ClientPackets.OpenProfile, PacketsHandler.OpenProfile},
            {(int) ClientPackets.GetLastSelectedTank, PacketsHandler.GetLastSelectedTank},
            {(int) ClientPackets.BuyTank, PacketsHandler.BuyTankRequest},
            {(int) ClientPackets.Ram, PacketsHandler.RamPlayer},
            {(int)ClientPackets.QuestsTime, PacketsHandler.HandleQuestsTime}
        };
        Log.Information("Packets initialized");
    }
}