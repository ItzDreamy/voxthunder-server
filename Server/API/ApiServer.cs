using System.Collections.Generic;

namespace VoxelTanksServer.API
{
    public static class ApiServer
    {
        public static int Port { get; private set; }
        public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();
    }
}