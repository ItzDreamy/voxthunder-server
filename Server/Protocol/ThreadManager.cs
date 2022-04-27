using Serilog;

namespace VoxelTanksServer.Protocol
{
    public static class ThreadManager
    {
        private static readonly List<Action> ExecuteOnMainThread = new();
        private static readonly List<Action> ExecuteCopiedOnMainThread = new();
        private static bool _actionToExecuteOnMainThread = false;
        
        public static void ExecuteInMainThread(Action action)
        {
            if (action == null)
            {
                Log.Information("No action to execute on main thread!");

                return;
            }

            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Add(action);
                _actionToExecuteOnMainThread = true;
            }
        }

        public static void UpdateMain()
        {
            if (_actionToExecuteOnMainThread)
            {
                ExecuteCopiedOnMainThread.Clear();
                lock (ExecuteOnMainThread)
                {
                    ExecuteCopiedOnMainThread.AddRange(ExecuteOnMainThread);
                    ExecuteOnMainThread.Clear();
                    _actionToExecuteOnMainThread = false;
                }

                foreach (var mainThread in ExecuteCopiedOnMainThread)
                {
                    mainThread();
                }
            }
        }
    }
}