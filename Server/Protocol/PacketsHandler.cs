using System.Numerics;
using Serilog;
using VoxelTanksServer.DB;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;

namespace VoxelTanksServer.Protocol;

public static class PacketsHandler
{
    public static void WelcomePacketReceived(int fromClient, Packet packet)
    {
        int clientIdCheck = packet.ReadInt();
        Log.Information(
            $"{Server.Clients[fromClient]?.Tcp?.Socket?.Client.RemoteEndPoint} connected successfully with ID {fromClient}");

        if (fromClient != clientIdCheck)
        {
            Log.Warning(
                $"Player (ID: {fromClient}) has the wrong client ID ({clientIdCheck})");
        }
    }

    public static void ReadyToSpawnReceived(int fromClient, Packet packet)
    {
        var player = Server.Clients[fromClient];

        if (!player.IsAuth)
        {
            player.Disconnect("Игрок не вошел в аккаунт");
        }

        if (player.Tcp.Socket == null) return;

        player.ReadyToSpawn = true;
        if (player.Reconnected)
        {
            player.SendIntoGame(player.Username, player.SelectedTank);
        }
    }

    public static void ChangeTank(int fromClient, Packet packet)
    {
        string? tankName = packet.ReadString();
        var client = Server.Clients[fromClient];

        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        //TODO: Check owned tanks
        bool isOwned = true;

        var tank = Server.Tanks.Find(tank =>
            string.Equals(tank.Name, tankName, StringComparison.CurrentCultureIgnoreCase));
        if (tank == null)
        {
            client.Disconnect("Неизвестный танк");
            return;
        }

        ServerSend.SwitchTank(client, tank, isOwned);
    }

    public static void GetPlayerMovement(int fromClient, Packet packet)
    {
        var client = Server.Clients[fromClient];

        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
            return;
        }

        var connectedRoom = client.ConnectedRoom;
        if (connectedRoom is {PlayersLocked: true})
        {
            client.Disconnect("Игрок разблокировал себя на стороне клиента (Движение)");
            return;
        }

        if (client.Player != null)
        {
            MovementData movement = packet.ReadMovement();
            client.Player.Position = movement.Position;
            client.Player.Rotation = movement.Rotation;
            ServerSend.SendMovementData(movement, connectedRoom, fromClient);
        }
    }

    public static void RotateTurret(int fromClient, Packet packet)
    {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        var connectedRoom = Server.Clients[fromClient].ConnectedRoom;
        if (connectedRoom is {PlayersLocked: true})
        {
            Server.Clients[fromClient].Disconnect("Игрок разблокировал себя на стороне клиента (Поворот башни)");
            return;
        }

        Quaternion turretRotation = packet.ReadQuaternion();
        Quaternion barrelRotation = packet.ReadQuaternion();

        Player? player = Server.Clients[fromClient].Player;
        player?.RotateTurret(turretRotation, barrelRotation);
    }

    public static async void TryLogin(int fromClient, Packet packet)
    {
        string? username = packet.ReadString();
        string? password = packet.ReadString();
        bool rememberUser = packet.ReadBool();
        var client = Server.Clients[fromClient];

        if (await AuthorizationHandler.TryLogin(username, password, rememberUser,
                client.Tcp.Socket.Client.RemoteEndPoint?.ToString(), fromClient))
        {
            var table = await DatabaseUtils.RequestData(
                $"SELECT Count(*) FROM `playerstats` WHERE `nickname` = '{client.Username}'");

            if ((long) table.Rows[0][0] <= 0)
            {
                await DatabaseUtils.ExecuteNonQuery(
                    $"INSERT INTO `playerstats` (`nickname`) VALUES ('{client.Username}')");
            }
        }
    }

    public static void InstantiateObject(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        string? name = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        ServerSend.InstantiateObject(name, position, rotation, fromClient,
            Server.Clients[fromClient].ConnectedRoom);
    }

    public static void ShootBullet(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        var connectedRoom = Server.Clients[fromClient].ConnectedRoom;
        if (connectedRoom is {PlayersLocked: true})
        {
            Server.Clients[fromClient].Disconnect("Игрок разблокировал себя на стороне клиента. (Стрельба)");
            return;
        }

        //Чтение данных префаба
        string? name = packet.ReadString();
        string? particlePrefab = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        Player? player = Server.Clients[fromClient].Player;
        //Выстрел
        player?.Shoot(name, particlePrefab, position, rotation);
    }

    public static void LeaveToLobby(int fromClient, Packet packet)
    {
        var client = Server.Clients[fromClient];

        ServerSend.LeaveToLobby(client.Id);
    }

    /// <summary>
    /// Получение урона
    /// </summary>
    /// <param name="fromClient"></param>
    /// <param name="packet"></param>
    public static void TakeDamage(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        //Чтение id врага
        int enemyId = packet.ReadInt();
        Player? enemy = Server.Clients[enemyId].Player;
        Player? hitPlayer = Server.Clients[fromClient].Player;

        if (enemy != null && hitPlayer != null && enemy.Team.Id != hitPlayer.Team.Id)
        {
            //Высчитывание урона
            int damage = enemy.SelectedTank.Damage;
            float randomCoof = new Random().Next(-20, 20) * ((float) damage / 100);
            int calculatedDamage = damage + (int) randomCoof;

            if (calculatedDamage == 0)
            {
                calculatedDamage = damage;
            }
            else if (calculatedDamage > hitPlayer.Health)
            {
                calculatedDamage = hitPlayer.Health;
            }

            //Подсчет суммарного урона врага
            enemy.TotalDamage += calculatedDamage;

            if (hitPlayer.Health > 0)
            {
                //Показ урона
                ServerSend.ShowDamage(enemy.Id, calculatedDamage, hitPlayer);
            }

            //Нанесение урона
            hitPlayer.TakeDamage(calculatedDamage, enemy);
            ServerSend.TakeDamageOtherPlayer(hitPlayer.ConnectedRoom, hitPlayer);
        }
    }

    /// <summary>
    /// Подключится или создать комнату, если нет доступной
    /// </summary>
    /// <param name="fromClient"></param>
    /// <param name="packet"></param>
    public static void JoinOrCreateRoom(int fromClient, Packet packet)
    {
        Client packetSender = Server.Clients[fromClient];
        if (!packetSender.IsAuth)
        {
            packetSender.Disconnect("Игрок не вошел в аккаунт");
        }

        //Поиск доступной комнаты, если существует - подключение
        if (Server.Rooms.Count > 0)
        {
            foreach (var room in Server.Rooms)
            {
                if (room.IsOpen)
                {
                    Client client = Server.Clients[fromClient];
                    if (client == null) return;
                    //Присоединение к комнате
                    client.JoinRoom(room);

                    //Закрытие комнаты и балансировка команд, если комната заполнена
                    if (room.PlayersCount == room.MaxPlayers)
                    {
                        room.IsOpen = false;
                        room.BalanceTeams();
                    }

                    return;
                }
            }
        }

        //Создание новой комнаты
        Room? newRoom = new Room(Server.Config.GeneralTime, Server.Config.PreparativeTime);
        //Присоединение к комнате
        Server.Clients[fromClient].JoinRoom(newRoom);

        //Закрытие комнаты и балансировка команд, если комната заполнена
        if (newRoom.PlayersCount == newRoom.MaxPlayers)
        {
            newRoom.IsOpen = false;
            newRoom.BalanceTeams();
        }
    }

    /// <summary>
    /// Покинуть комнату
    /// </summary>
    /// <param name="fromClient"></param>
    /// <param name="packet"></param>
    public static void LeaveRoom(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        Room? playerRoom = Server.Clients[fromClient].ConnectedRoom;
        if (playerRoom is {IsOpen: true})
        {
            Server.Clients[fromClient].LeaveRoom();
        }
    }

    /// <summary>
    /// Проверка на доступность переподключения к игре
    /// </summary>
    /// <param name="fromClient"></param>
    /// <param name="packet"></param>
    public static void CheckAbleToReconnect(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        //Поиск игрока в кеше комнат
        foreach (var room in Server.Rooms.Where(room => room is {IsOpen: false}))
        {
            foreach (var cachedPlayer in room?.CachedPlayers.Where(cachedPlayer =>
                         cachedPlayer?.Username.ToLower() == Server.Clients[fromClient].Username?.ToLower() &&
                         cachedPlayer.IsAlive && !room.GameEnded))
            {
                ServerSend.AbleToReconnect(fromClient);
                Log.Information($"{Server.Clients[fromClient].Username} can reconnect to battle");
            }
        }
    }

    public static void Reconnect(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        foreach (var room in Server.Rooms.Where(room => room is {IsOpen: false}))
        {
            var cachedPlayer = room?.CachedPlayers.Find(player =>
                player?.Username?.ToLower() == Server.Clients[fromClient].Username?.ToLower());
            if (cachedPlayer == null)
            {
                return;
            }

            client.JoinRoom(room);
            client.Team = cachedPlayer?.Team;
            client?.Team?.Players.Add(client);
            client.Reconnected = true;
            client.Player = new Player(cachedPlayer, fromClient);
            ServerSend.LoadScene(fromClient, room.Map.Name);
        }
    }

    public static void CancelReconnect(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
        }

        foreach (var room in Server.Rooms)
        {
            foreach (var cachedPlayer in room.CachedPlayers)
            {
                if (cachedPlayer?.Username == Server.Clients[fromClient].Username)
                {
                    Log.Information($"{cachedPlayer?.Username} canceled reconnect");
                    room.CachedPlayers[room.CachedPlayers.IndexOf(cachedPlayer)] = null;
                    return;
                }
            }
        }
    }

    public static void RequestPlayersStats(int fromClient, Packet packet)
    {
        Room room = Server.Clients[fromClient].ConnectedRoom;

        if (room?.Players == null)
        {
            return;
        }

        ServerSend.SendPlayersStats(room);
    }

    public static void HandleProfileRequest(int fromClient, Packet packet)
    {
        Client client = Server.Clients[fromClient];
        if (!client.IsAuth)
        {
            client.Disconnect("Игрок не вошел в аккаунт");
            return;
        }

        ServerSend.SendProfileData(client);
    }

    public static async void AuthById(int fromClient, Packet packet)
    {
        string id = packet.ReadString();
        bool isAuth = await DatabaseUtils.TryLoginByToken(id, fromClient);
        ServerSend.LoginResult(fromClient, isAuth, isAuth ? "Авторизация прошла успешно" : "Сессия завершина");
    }

    public static void SignOut(int fromClient, Packet packet)
    {
        var client = Server.Clients[fromClient];
        client.IsAuth = false;
        client.Username = null;
        ServerSend.SignOut(fromClient);
    }
}