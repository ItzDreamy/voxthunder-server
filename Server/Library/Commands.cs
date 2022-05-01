using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Library;

public static class Commands
{
    public static void ShowOnline()
    {
        Console.WriteLine($"Current online: {Server.OnlinePlayers} / {Server.MaxPlayers}");
    }

    public static void KickPlayer()
    {
        Console.Write("Player name: ");
        var nickname = Console.ReadLine();
        if (TryGetClient(nickname, out var client))
        {
            client.Disconnect("User kicked");
        }
    }

    public static void BanPlayer()
    {
        Console.Write("Player name: ");
        var nickname = Console.ReadLine();
        if (TryGetClient(nickname, out var client))
        {
            client.Disconnect("User banned");
        }
    }

    public static void StopServer()
    {
        Environment.Exit(0);
    }

    public static void ShowInfo()
    {
        Console.WriteLine(
            $"Server for VoxThunder.\nClient version: {Server.Config.ClientVersion}\nOnline: {Server.OnlinePlayers} / {Server.Config.MaxPlayers}\nRooms count: {Server.Rooms.Count}\nPlayers per room: {Server.Config.MaxPlayersInRoom}\nDiscord server: https://discord.gg/Pjs6HKA3vz");
    }

    public static void ShowCommandList()
    {
        Console.WriteLine(
            "**players** - show list of online players\n**online** - show count of online players\n**smp_room** - set max players in room\n**kick** - kick player\n**ban** - ban player\n**stop** - stop server");
    }

    public static void ShowPlayerList()
    {
        Console.WriteLine("Player list: ");
        foreach (var client in Server.Clients.Values.Where(client => client.IsAuth))
        {
            Console.WriteLine(client.Data.Username);
        }
    }

    public static void SetMaxPlayersInRoom()
    {
        Console.Write("Введите кол-во игроков в комнате: ");
        try
        {
            int playersCount = int.Parse(Console.ReadLine());
            if (playersCount % 2 != 0)
            {
                Console.WriteLine("Введите четное число игроков!");
                return;
            }

            Server.Config.MaxPlayersInRoom = playersCount;
        }
        catch
        {
            Console.WriteLine("Не удалось преобразовать строку в число.");
        }
    }

    private static bool TryGetClient(string? username, out Client client)
    {
        client = Server.Clients.Values.ToList().Find(c => c?.Data.Username?.ToLower() == username.ToLower());
        Console.Write(client == null ? "Player not found\n" : null);
        return client != null;
    }
}