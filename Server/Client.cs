using System;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;
using Serilog;
using VoxelTanksServer.GameCore;

namespace VoxelTanksServer
{
    public class Client
    {
        public delegate void RoomHandler(Room room);
        public event RoomHandler OnJoinedRoom;
        public event RoomHandler OnLeftRoom;
        
        //Размер принимающих/отправляемых данных
        public static int DataBufferSize = 4096;

        public int Id;
        public bool IsAuth = false;
        public Player? Player;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool ReadyToSpawn;

        public string? Username;
        public Tank SelectedTank;

        public Room? ConnectedRoom = null;
        public Team? Team = null;

        public TCP Tcp;

        private int _afkTimer = Server.AfkTime;

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
                int packetLength = 0;

                _receivedData.SetBytes(data);

                //Считывать id пакета, если длина пакета <= 0, то сбрасывать пакет 
                if (_receivedData.UnreadLength() >= 4)
                {
                    packetLength = _receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }

                //Считывание данных из пакета
                while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
                {
                    byte[] packetBytes = _receivedData.ReadBytes(packetLength);

                    //Считывание id пакета и вызов соответствующего ему метод
                    ThreadManager.ExecuteInMainThread(() =>
                    {
                        using (Packet packet = new(packetBytes))
                        {
                            try
                            {
                                int packetId = packet.ReadInt();
                                Server.PacketHandlers[packetId](_id, packet);

                                Server.Clients[_id]._afkTimer = Server.AfkTime;
                            }
                            catch (Exception e)
                            {
                                Server.Clients[_id].Disconnect("Со стороны клиента пришел некорректный пакет.");
                                Log.Information($"Со стороны клиента пришел некорректный пакет. Error: {e}");
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

            /// <summary>
            /// Отключение клиента от сервера
            /// </summary>
            public void Disconnect()
            {
                //Закрытие сокета
                Socket?.Close();
                //Сброс сетевых настроек
                _stream = null;
                _receivedData = null;
                _receiveBuffer = null;
                Socket = null;
            }

            /// <summary>
            /// Отклик на получение данных
            /// </summary>
            /// <param name="result"></param>
            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    if (_stream is {CanRead: true})
                    {
                        int byteLength = _stream.EndRead(result);
                        //Если данные пустые, то отключать клиент от сервера
                        if (byteLength <= 0)
                        {
                            Server.Clients[_id].Disconnect("Ошибка получения данных клиента");
                            return;
                        }
                        //Обработка данных и повторный запуск чтения
                        byte[] data = new byte[byteLength];
                        Array.Copy(_receiveBuffer, data, byteLength);

                        _receivedData.Reset(HandleData(data));
                        _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);   
                    }
                }
                //При ошибке отключать клиент от сервера
                catch (Exception e)
                {
                    Log.Error($"Error receiving TCP data: {e}");
                    Server.Clients[_id].Disconnect("Ошибка получения данных клиента");
                }
            }

            /// <summary>
            /// Отправка данных клиенту
            /// </summary>
            /// <param name="packet">Пакет данных</param>
            public void SendData(Packet packet)
            {
                try
                {
                    if (Socket != null)
                    {
                        //Запись данных в поток
                        _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                //При ошибке отключать клиент от сервера
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
        
        /// <summary>
        /// Отправление клиента в игру (спавн всех игроков)
        /// </summary>
        /// <param name="playerName">Никнейм игрока</param>
        /// <param name="tank">Выбранный танк игрока</param>
        public void SendIntoGame(string? playerName, Tank tank)
        {
            //Создавать новый экземпляр игрока, если он не существует
            Player ??= new Player(Id, playerName, Position, Rotation, tank, ConnectedRoom);
            Player.Team = Team;

            //Спавн остальных игроков в комнате для данного клиента
            foreach (var client in ConnectedRoom.Players.Values.Where(client => client.Player != null).Where(client => client.Id != Id))
            {
                ServerSend.SpawnPlayer(Id, client.Player);
            }
            //Спавн клиента в комнате для других игроков
            foreach (var client in ConnectedRoom.Players.Values.Where(client => client.Player != null))
            {
                ServerSend.SpawnPlayer(client.Id, Player);
            }
        }

        /// <summary>
        /// Отключение клиента от сервера
        /// </summary>
        /// <param name="reason">Причина отключения</param>
        public void Disconnect(string reason)
        {
            if (Tcp?.Socket == null)
                return;

            Log.Information($"{Tcp.Socket?.Client?.RemoteEndPoint} отключился. Причина: {reason}");
            ServerSend.PlayerDisconnected(Id, ConnectedRoom);

            //Кешировать игрока и покидать комнату, если он находился в ней
            if(ConnectedRoom != null)
            {
                if (Player != null)
                {
                    int playerIndex = Player.ConnectedRoom.CachedPlayers.IndexOf(
                            Player.ConnectedRoom.CachedPlayers.Find(cachedPlayer => string.Equals(cachedPlayer?.Username, Username, StringComparison.CurrentCultureIgnoreCase)));
                    //Cache player
                    if (playerIndex != -1)
                        Player.ConnectedRoom.CachedPlayers[playerIndex] = Player.CachePlayer();
                }
                
                LeaveRoom();
            }

            //Отписка от событий
            OnJoinedRoom -= ServerSend.ShowPlayersCountInRoom;
            OnLeftRoom -= ServerSend.ShowPlayersCountInRoom;

            //Сброс клиента для дальнейшего использования
            _afkTimer = Server.AfkTime;
            Player = null;
            Username = null;
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            SelectedTank = null;
            IsAuth = false;
            ReadyToSpawn = false;

            //Отключение от сети
            Tcp.Disconnect();
        }

        /// <summary>
        /// Подключится к указаной комнате
        /// </summary>
        /// <param name="room">Комната</param>
        public void JoinRoom(Room room)
        {
            room.Players[Id] = this;
            this.ConnectedRoom = room;

            //Вызов события
            OnJoinedRoom?.Invoke(ConnectedRoom);
        }

        /// <summary>
        /// Покинуть комнату
        /// </summary>
        public void LeaveRoom()
        {
            //Удаление игрока из комнаты и команды
            ConnectedRoom?.Players.Remove(Id);
            Team?.Players.Remove(this);
            
            //Если в комнате не осталось игроков - удалять её
            if (ConnectedRoom?.PlayersCount == 0)
            {
                Server.Rooms.Remove(ConnectedRoom);
            }

            //Вызов события
            if (ConnectedRoom != null)
            {
                OnLeftRoom?.Invoke(ConnectedRoom);
            }

            //Сброс комнаты и команды у клиента
            ConnectedRoom = null;
            Team = null;
            Player = null;
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            ReadyToSpawn = false;
        }
    }
}