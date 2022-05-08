using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public static class GameLogic
{
    public static void Update()
    {
        ThreadManager.UpdateMain();
    }
}