using System;
using System.Net.Sockets;

namespace VoxelTanksServer
{
    public class Client
    {
        public static int DataBufferSize = 4096;
        
        public int Id;
        public TCP Tcp;

        public Client(int clientId)
        {
            Id = clientId;
            Tcp = new TCP(Id);
        }
        
        public class TCP
        {
            public TcpClient Socket;

            private readonly int _id;
            private NetworkStream _stream;
            private byte[] _receiveBuffer;

            public TCP(int id)
            {
                id = _id;
            }

            public void Connect(TcpClient socket)
            {
                Socket = socket;
                Socket.ReceiveBufferSize = DataBufferSize;
                Socket.SendBufferSize = DataBufferSize;

                _stream = Socket.GetStream();
                _receiveBuffer = new byte[DataBufferSize];

                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                
                //TODO: send welcome packet
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = _stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        //TODO disconnect
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);
                    //TODO: handle data
                    _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error receiving TCP data: {e.Message}");
                    //TODO: disconnect
                }
            }
        }
    }
}