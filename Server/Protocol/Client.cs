using System.Net.Sockets;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VoxelTanksServer.Database;
using VoxelTanksServer.Database.Models;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;

namespace VoxelTanksServer.Protocol;

public class Client {
    public delegate void RoomHandler(Room room);

    private const int DataBufferSize = 4096;
    public readonly int Id;

    private int _afkTimer = Server.Config.AfkTime;

    public Room? ConnectedRoom;

    public PlayerData Data;
    public bool IsAuth;
    public Player? Player;
    public bool ReadyToSpawn;
    public bool Reconnected;

    public Tank SelectedTank;
    public Vector3 SpawnPosition;
    public Quaternion SpawnRotation;

    public TCP Tcp;
    public Team? Team;

    public Client(int clientId) {
        Id = clientId;
        Tcp = new TCP(Id);
    }

    public event RoomHandler OnJoinedRoom;
    public event RoomHandler OnLeftRoom;

    private void StartAfkTimer() {
        Task.Run(async () => {
            while (_afkTimer > 0 && Tcp.Socket != null) {
                await Task.Delay(1000);
                _afkTimer -= 1000;
            }

            if (Tcp.Socket != null) Disconnect("AFK");
        });
    }

    public void SendIntoGame(string? playerName, Tank tank) {
        Player ??= new Player(Id, playerName, SpawnPosition, SpawnRotation, tank, ConnectedRoom);
        Player.Team = Team;

        foreach (var client in ConnectedRoom?.Players.Values.Where(client => client?.Player != null)
                     .Where(client => client.Id != Id)!)
            ServerSend.SpawnPlayer(Id, client.Player);

        foreach (var client in ConnectedRoom.Players.Values.Where(client => client?.Player != null))
            ServerSend.SpawnPlayer(client.Id, Player);
    }

    public void Disconnect(string reason = "") {
        if (Tcp?.Socket == null)
            return;

        if (Data != null && Data.Nickname != null) {
            var databaseData = (Server.DatabaseService.Context.playerstats.ToList())
                .Find(data => string.Equals(data.Nickname, Data.Nickname, StringComparison.CurrentCultureIgnoreCase));
            databaseData = (PlayerData) Data.Clone();
            Server.DatabaseService.Context.SaveChanges();
        }
        
        reason = reason == string.Empty ? "" : $"Причина: {reason}";
        Log.Information($"{Tcp.Socket?.Client?.RemoteEndPoint} отключился. {reason}");
        ServerSend.PlayerDisconnected(Id, ConnectedRoom);

        if (ConnectedRoom != null) {
            if (Player != null) {
                var playerIndex = Player.ConnectedRoom.CachedPlayers.IndexOf(
                    Player.ConnectedRoom.CachedPlayers.Find(cachedPlayer =>
                        string.Equals(cachedPlayer?.Username, Data.Nickname,
                            StringComparison.CurrentCultureIgnoreCase)));
                if (playerIndex != -1)
                    Player.ConnectedRoom.CachedPlayers[playerIndex] = Player.CachePlayer();
            }

            LeaveRoom();
        }

        OnJoinedRoom -= ServerSend.ShowPlayersCountInRoom;
        OnLeftRoom -= ServerSend.ShowPlayersCountInRoom;

        _afkTimer = Server.Config.AfkTime;
        Reconnected = false;
        Player = null;
        SpawnPosition = Vector3.Zero;
        SpawnRotation = Quaternion.Identity;
        SelectedTank = null;
        IsAuth = false;
        ReadyToSpawn = false;
        Data = default;

        Tcp.Disconnect();
    }

    public void JoinRoom(Room room) {
        room.Players[Id] = this;
        ConnectedRoom = room;

        OnJoinedRoom?.Invoke(ConnectedRoom);
    }

    public void LeaveRoom() {
        ConnectedRoom?.Players.Remove(Id);
        Team?.Players.Remove(this);

        if (ConnectedRoom?.PlayersCount == 0) Server.Rooms.Remove(ConnectedRoom);

        if (ConnectedRoom != null) OnLeftRoom?.Invoke(ConnectedRoom);

        Reconnected = false;
        ConnectedRoom = null;
        Team = null;
        Player = null;
        SpawnPosition = Vector3.Zero;
        SpawnRotation = Quaternion.Identity;
        ReadyToSpawn = false;
    }

    public class TCP {
        private readonly int _id;
        private byte[] _receiveBuffer;
        private Packet _receivedData;
        private NetworkStream _stream;
        public TcpClient? Socket;

        public TCP(int id) {
            _id = id;
        }

        public void Connect(TcpClient socket) {
            Socket = socket;
            Socket.ReceiveBufferSize = DataBufferSize;
            Socket.SendBufferSize = DataBufferSize;

            _stream = Socket.GetStream();

            _receivedData = new Packet();
            _receiveBuffer = new byte[DataBufferSize];

            _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

            var client = Server.Clients[_id];

            client.OnJoinedRoom += ServerSend.ShowPlayersCountInRoom;
            client.OnLeftRoom += ServerSend.ShowPlayersCountInRoom;
            ServerSend.Welcome(_id, "You have been successfully connected to server");
            client.StartAfkTimer();
        }

        private bool HandleData(byte[] data) {
            try {
                var packetLength = 0;

                _receivedData.SetBytes(data);

                if (_receivedData.UnreadLength() >= 4) {
                    packetLength = _receivedData.ReadInt();
                    if (packetLength <= 0) return true;
                }

                while (packetLength > 0 && packetLength <= _receivedData.UnreadLength()) {
                    var packetBytes = _receivedData.ReadBytes(packetLength);

                    ThreadManager.ExecuteInMainThread(() => {
                        using (Packet packet = new(packetBytes)) {
                            try {
                                var packetId = packet.ReadInt();
                                if (Server.PacketHandlers.ContainsKey(packetId)) {
                                    Server.PacketHandlers[packetId](_id, packet);
                                    Server.Clients[_id]._afkTimer = Server.Config.AfkTime;
                                }
                            }
                            catch (Exception e) {
                                Server.Clients[_id].Disconnect("Со стороны клиента пришел некорректный пакет.");
                                Log.Error($"Unhandled packet. Error: {e}");
                            }
                        }
                    });

                    packetLength = 0;

                    if (_receivedData.UnreadLength() >= 4) {
                        packetLength = _receivedData.ReadInt();
                        if (packetLength <= 0) return true;
                    }
                }

                if (packetLength <= 1) return true;

                return false;
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            return true;
        }

        public void Disconnect() {
            Socket?.Close();
            _stream = null;
            _receivedData = null;
            _receiveBuffer = null;
            Socket = null;
        }

        private void ReceiveCallback(IAsyncResult result) {
            try {
                if (_stream is {CanRead: true}) {
                    var byteLength = _stream.EndRead(result);
                    if (byteLength <= 0) {
                        Server.Clients[_id].Disconnect();
                        return;
                    }

                    var data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);

                    _receivedData.Reset(HandleData(data));
                    _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                }
            }
            catch (Exception e) {
                Log.Error($"Error receiving TCP data: {e}");
                Server.Clients[_id].Disconnect("Ошибка получения данных клиента");
            }
        }

        public void SendData(Packet packet) {
            try {
                if (Socket != null) _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
            }
            catch (Exception e) {
                Log.Error($"Error sending data to player {_id} via TCP {e}");
                Server.Clients[_id].Disconnect("Ошибка отправки данных клиенту");
            }
        }
    }
}