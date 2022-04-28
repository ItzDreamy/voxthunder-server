using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public static class GameLogic
{
    //Обновление сервера
    public static void Update()
    {
        ThreadManager.UpdateMain();
    }
}