﻿using System;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;
using Serilog;
using VoxelTanksServer.GameCore;

namespace VoxelTanksServer.Protocol
{
    public class Client
    {
        public delegate void RoomHandler(Room room);
        public event RoomHandler OnJoinedRoom;
        public event RoomHandler OnLeftRoom;

        private const int DataBufferSize = 4096;

        public readonly int Id;
        public bool IsAuth = false;
        public bool Reconnected = false;
        public Player? Player;
        public Vector3 SpawnPosition;
        public Quaternion SpawnRotation;
        public bool ReadyToSpawn;

        public string? Username;
        public Tank SelectedTank;

        public Room? ConnectedRoom = null;
        public Team? Team = null;

        public TCP Tcp;

        private int _afkTimer = Server.Config.AfkTime;

        /// <summary>
        /// Создание экземпляра клиента
        /// </summary>
        /// <param name="clientId"></param>
        public Client(int clientId)
        {
            Id = clientId;
            Tcp = new TCP(Id);
        }

        /// <summary>
        /// Класс для взаимодействия с клиентом по сети
        /// </summary>
        public class TCP
        {
            public TcpClient? Socket;

            private readonly int _id;
            private NetworkStream _stream;
            private Packet _receivedData;
            private byte[] _receiveBuffer;

            public TCP(int id)
            {
                _id = id;
            }

            /// <summary>
            /// Подключение клиента к серверу
            /// </summary>
            /// <param name="socket">Сокет клиента</param>
            public void Connect(TcpClient socket)
            {
                //Инициализация сокета
                Socket = socket;
                Socket.ReceiveBufferSize = DataBufferSize;
                Socket.SendBufferSize = DataBufferSize;

                //Получение потока данных
                _stream = Socket.GetStream();

                //Создания буфера для получаемых данных
                _receivedData = new Packet();
                _receiveBuffer = new byte[DataBufferSize];

                //Начать чтения данных клиента
                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

                //Подписка на необходимые события
                Server.Clients[_id].OnJoinedRoom += ServerSend.ShowPlayersCountInRoom;
                Server.Clients[_id].OnLeftRoom += ServerSend.ShowPlayersCountInRoom;

                ServerSend.Welcome(_id, "You have been successfully connected to server");

                Server.Clients[_id].StartAfkTimer();
            }

            /// <summary>
            /// Обработка получаемых данных
            /// </summary>
            /// <param name="data">Данные</param>
            /// <returns>Сбрасывать ли настройки экземпляр пакета для повторного использования?</returns>
            private bool HandleData(byte[] data)
            {
                try
                {
                    int packetLength = 0;

                    _receivedData.SetBytes(data);

                    if (_receivedData.UnreadLength() >= 4)
                    {
                        packetLength = _receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }

                    while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
                    {
                        byte[] packetBytes = _receivedData.ReadBytes(packetLength);

                        ThreadManager.ExecuteInMainThread(() =>
                        {
                            using (Packet packet = new(packetBytes))
                            {
                                try
                                {
                                    int packetId = packet.ReadInt();
                                    if (Server.PacketHandlers.ContainsKey(packetId))
                                    {
                                        Server.PacketHandlers[packetId](_id, packet);
                                        Server.Clients[_id]._afkTimer = Server.Config.AfkTime;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Server.Clients[_id].Disconnect("Со стороны клиента пришел некорректный пакет.");
                                    Log.Error($"Unhandled packet. Error: {e}");
                                }
                            }
                        });

                        packetLength = 0;

                        if (_receivedData.UnreadLength() >= 4)
                        {
                            packetLength = _receivedData.ReadInt();
                            if (packetLength <= 0)
                            {
                                return true;
                            }
                        }
                    }

                    if (packetLength <= 1)
                    {
                        return true;
                    }

                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public void Disconnect()
            {
                //Закрытие сокета
                Socket?.Close();
                _stream = null;
                _receivedData = null;
                _receiveBuffer = null;
                Socket = null;
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    if (_stream is {CanRead: true})
                    {
                        int byteLength = _stream.EndRead(result);
                        if (byteLength <= 0)
                        {
                            Server.Clients[_id].Disconnect("Ошибка получения данных клиента");
                            return;
                        }

                        byte[] data = new byte[byteLength];
                        Array.Copy(_receiveBuffer, data, byteLength);

                        _receivedData.Reset(HandleData(data));
                        _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error receiving TCP data: {e}");
                    Server.Clients[_id].Disconnect("Ошибка получения данных клиента");
                }
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (Socket != null)
                    {
                        _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error sending data to player {_id} via TCP {e}");
                    Server.Clients[_id].Disconnect("Ошибка отправки данных клиенту");
                }
            }
        }

        private void StartAfkTimer()
        {
            Task.Run(async () =>
            {
                while (_afkTimer > 0 && Tcp.Socket != null)
                {
                    await Task.Delay(1000);
                    _afkTimer -= 1000;
                }

                if (Tcp.Socket != null)
                {
                    Disconnect("AFK");
                }
            });
        }

        public void SendIntoGame(string? playerName, Tank tank)
        {
            Player ??= new Player(Id, playerName, SpawnPosition, SpawnRotation, tank, ConnectedRoom);
            Log.Information(Player.Position.ToString());
            Player.Team = Team;

            foreach (var client in ConnectedRoom?.Players.Values.Where(client => client?.Player != null)
                .Where(client => client.Id != Id)!)
            {
                ServerSend.SpawnPlayer(Id, client.Player);
            }

            foreach (var client in ConnectedRoom.Players.Values.Where(client => client?.Player != null))
            {
                ServerSend.SpawnPlayer(client.Id, Player);
            }
        }

        public void Disconnect(string reason)
        {
            if (Tcp?.Socket == null)
                return;

            Log.Information($"{Tcp.Socket?.Client?.RemoteEndPoint} отключился. Причина: {reason}");
            ServerSend.PlayerDisconnected(Id, ConnectedRoom);

            if (ConnectedRoom != null)
            {
                if (Player != null)
                {
                    int playerIndex = Player.ConnectedRoom.CachedPlayers.IndexOf(
                        Player.ConnectedRoom.CachedPlayers.Find(cachedPlayer =>
                            string.Equals(cachedPlayer?.Username, Username,
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
            Username = null;
            SpawnPosition = Vector3.Zero;
            SpawnRotation = Quaternion.Identity;
            SelectedTank = null;
            IsAuth = false;
            ReadyToSpawn = false;

            Tcp.Disconnect();
        }

        public void JoinRoom(Room room)
        {
            room.Players[Id] = this;
            this.ConnectedRoom = room;

            OnJoinedRoom?.Invoke(ConnectedRoom);
        }

        public void LeaveRoom()
        {
            ConnectedRoom?.Players.Remove(Id);
            Team?.Players.Remove(this);

            if (ConnectedRoom?.PlayersCount == 0)
            {
                Server.Rooms.Remove(ConnectedRoom);
            }

            if (ConnectedRoom != null)
            {
                OnLeftRoom?.Invoke(ConnectedRoom);
            }

            Reconnected = false;
            ConnectedRoom = null;
            Team = null;
            Player = null;
            SpawnPosition = Vector3.Zero;
            SpawnRotation = Quaternion.Identity;
            ReadyToSpawn = false;
        }
    }
}