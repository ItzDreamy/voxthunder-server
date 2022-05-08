using System.Net.Sockets;
using Serilog;

namespace VoxelTanksServer.Protocol.API;

public class ApiClient {
    public static int DataBufferSize = 4096;

    public int Id;
    public TCP Tcp;

    public ApiClient(int clientId) {
        Id = clientId;
        Tcp = new TCP(Id);
    }

    public void Disconnect() {
        if (Tcp.Socket == null)
            return;
        Tcp.Disconnect();

        var client = Server.Clients.Values.ToList().Find(client => {
            if (client.Tcp.Socket != null && Tcp.Socket != null)
                return client.Tcp.Socket.Client.RemoteEndPoint.ToString() ==
                       Tcp.Socket.Client.RemoteEndPoint.ToString();

            return false;
        });

        if (client != null) client.Disconnect("Api отключен");
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

        public void Connect(TcpClient? socket) {
            Socket = socket;
            Socket.ReceiveBufferSize = DataBufferSize;
            Socket.SendBufferSize = DataBufferSize;

            _stream = Socket.GetStream();

            _receivedData = new Packet();
            _receiveBuffer = new byte[DataBufferSize];

            _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

            var players = 0;

            foreach (var client in Server.Clients.Values)
                if (client.Tcp.Socket != null)
                    players++;
            ApiSend.WelcomePacket(_id);
        }

        private bool HandleData(byte[] data) {
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
                        var packetId = packet.ReadInt();
                        ApiServer.PacketHandlers[packetId](_id, packet);
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

        public void Disconnect() {
            Socket.Close();
            _stream = null;
            _receivedData = null;
            _receiveBuffer = null;
            Socket = null;
        }

        private void ReceiveCallback(IAsyncResult result) {
            try {
                if (_stream != null && _stream.CanRead) {
                    var byteLength = _stream.EndRead(result);
                    if (byteLength <= 0) {
                        ApiServer.Clients[_id].Disconnect();
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
                ApiServer.Clients[_id].Disconnect();
            }
        }

        public void SendData(Packet packet) {
            try {
                if (_stream != null && _stream.CanWrite && Socket != null)
                    _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
            }
            catch (Exception e) {
                Log.Error($"Error sending data to player {_id} via TCP {e}");
                ApiServer.Clients[_id].Disconnect();
            }
        }
    }
}