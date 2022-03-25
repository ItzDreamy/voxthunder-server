namespace VoxelTanksServer
{
    public static class GameLogic
    {
        //Обновление сервера
        public static void Update()
        {
            ThreadManager.UpdateMain();
        }
    }
}