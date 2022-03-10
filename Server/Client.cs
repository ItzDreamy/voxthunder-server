using System;
using System.Net.Sockets;
using System.Numerics;
using Serilog;

namespace VoxelTanksServer
{
    public class Client
    {
        public static int DataBufferSize = 4096;

        public int Id;
        public bool IsAuth = false;
        public Player? Player;
        public string? Username;
        public string? SelectedTank;

        public Room? ConnectedRoom = null;
        public Team? Team = null;

        public TCP Tcp;

        public Client(int clientId)
        {
            Id = clientId;
            Tcp = new TCP(Id);
        }

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

            public void Connect(TcpClient? socket)
            {
                Socket = socket;
                Socket.ReceiveBufferSize = DataBufferSize;
                Socket.SendBufferSize = DataBufferSize;

                _stream = Socket.GetStream();

                _receivedData = new Packet();
                _receiveBuffer = new byte[DataBufferSize];

                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(_id, "You have been successfully connected to server");
            }

            private bool HandleData(byte[] data)
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
                        using (Packet packet = new Packet(packetBytes))
                        {
                            int packetId = packet.ReadInt();
                            Server.PacketHandlers[packetId](_id, packet);
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

            public void Disconnect()
            {
                Socket.Close();
                _stream = null;
                _receivedData = null;
                _receiveBuffer = null;
                Socket = null;
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    if (_stream != null)
                    {
                        int byteLength = _stream.EndRead(result);
                        if (byteLength <= 0)
                        {
                            Server.Clients[_id].Disconnect();
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
                    Server.Clients[_id].Disconnect();
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
                    Log.Error($"Error sending data to player {_id} via TCP {e.Message}");
                    Server.Clients[_id].Disconnect();
                }
            }
        }

        public void SendIntoGame(string? playerName, string? tankName)
        {
            Player ??= new Player(Id, playerName, new Vector3(0, 0, 0), tankName, ConnectedRoom);
            Player.Team = Team;
            foreach (var client in ConnectedRoom.Players.Values)
            {
                if (client.Player != null)
                {
                    if (client.Id != Id)
                    {
                        ServerSend.SpawnPlayer(Id, client.Player);
                    }
                }
            }

            foreach (var client in ConnectedRoom.Players.Values)
            {
                if (client.Player != null)
                {
                    ServerSend.SpawnPlayer(client.Id, Player);
                }
            }
        }

        public void Disconnect()
        {
            if (Tcp.Socket == null)
                return;
            Log.Information($"{Tcp.Socket.Client.RemoteEndPoint} отключился.");
            ServerSend.PlayerDisconnected(Id, ConnectedRoom,false);
            LeaveRoom();
            Player = null;
            Username = null;
            SelectedTank = null;
            IsAuth = false;
            Tcp.Disconnect();
        }

        public void LeaveRoom()
        {
            if (ConnectedRoom == null) return;
            ConnectedRoom.Players.Remove(Id);
            if (Team != null)
            {
                Team.Players.Remove(this);
                Team = null;
            }
            if (ConnectedRoom.PlayersCount == 0)
            {
                Server.Rooms.Remove(ConnectedRoom);
            }
            ConnectedRoom = null;
        }
    }
}