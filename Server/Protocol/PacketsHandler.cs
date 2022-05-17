using Serilog;
using VoxelTanksServer.DB;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;

namespace VoxelTanksServer.Protocol;

public static class PacketsHandler {
    public static void WelcomePacketReceived(int fromClient, Packet packet) {
        var clientIdCheck = packet.ReadInt();
        Log.Information(
            $"{Server.Clients[fromClient]?.Tcp?.Socket?.Client.RemoteEndPoint} connected successfully with ID {fromClient}");

        if (fromClient != clientIdCheck)
            Log.Warning(
                $"Player (ID: {fromClient}) has the wrong client ID ({clientIdCheck})");
    }

    public static void ReadyToSpawnReceived(int fromClient, Packet packet) {
        var player = Server.Clients[fromClient];

        if (!player.IsAuth) player.Disconnect("Игрок не вошел в аккаунт");

        if (player.Tcp.Socket == null) return;

        player.ReadyToSpawn = true;
        if (player.Reconnected) player.SendIntoGame(player.Data.Username, player.SelectedTank);
    }

    public static async void ChangeTank(int fromClient, Packet packet) {
        var tankName = packet.ReadString();
        var client = Server.Clients[fromClient];

        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        var table = await DatabaseUtils.RequestData(
            $"SELECT Count(*) FROM `playerstats` WHERE `nickname` = '{client.Data.Username}' AND `{tankName}` = 1");

        var isOwned = (long) table.Rows[0][0] > 0;

        var tank = Server.Tanks.Find(tank =>
            string.Equals(tank.Name, tankName, StringComparison.CurrentCultureIgnoreCase));
        if (tank == null) {
            client.Disconnect("Неизвестный танк");
            return;
        }

        ServerSend.SwitchTank(client, tank, isOwned);
    }

    public static void GetPlayerMovement(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];

        if (!client.IsAuth) {
            client.Disconnect("Игрок не вошел в аккаунт");
            return;
        }

        var connectedRoom = client.ConnectedRoom;
        if (connectedRoom is {PlayersLocked: true}) {
            client.Disconnect("Игрок разблокировал себя на стороне клиента (Движение)");
            return;
        }

        if (client.Player != null) {
            var movement = packet.ReadMovement();
            client.Player.Movement = movement;
            ServerSend.SendMovementData(movement, connectedRoom, fromClient);
        }
    }

    public static void RotateTurret(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        var connectedRoom = Server.Clients[fromClient].ConnectedRoom;
        if (connectedRoom is {PlayersLocked: true}) {
            Server.Clients[fromClient].Disconnect("Игрок разблокировал себя на стороне клиента (Поворот башни)");
            return;
        }

        var turretRotation = packet.ReadQuaternion();
        var barrelRotation = packet.ReadQuaternion();

        var player = Server.Clients[fromClient].Player;
        player?.RotateTurret(turretRotation, barrelRotation);
    }

    public static async void TryLogin(int fromClient, Packet packet) {
        var username = packet.ReadString();
        var password = packet.ReadString();
        var rememberUser = packet.ReadBool();
        var client = Server.Clients[fromClient];

        if (client.IsAuth) return;
        
        if (await AuthorizationHandler.TryLogin(username, password, rememberUser,
                client.Tcp.Socket.Client.RemoteEndPoint?.ToString(), fromClient)) {
            var table = await DatabaseUtils.RequestData(
                $"SELECT Count(*) FROM `playerstats` WHERE `nickname` = '{client.Data.Username}'");

            if ((long) table.Rows[0][0] <= 0)
                await DatabaseUtils.ExecuteNonQuery(
                    $"INSERT INTO `playerstats` (`nickname`, `rankID`, `raider`, `selectedTank`) VALUES ('{client.Data.Username}', 1, 1, 'raider')");

            client.Data = await DatabaseUtils.GetPlayerData(client);
            ServerSend.SendPlayerData(client);
        }
    }

    public static async void GetLastSelectedTank(int fromClient, Packet packet) {
        var table = await DatabaseUtils.RequestData(
            $"SELECT `selectedTank` FROM `playerstats` WHERE `nickname` = '{Server.Clients[fromClient].Data.Username}'");
        var selectedTankName = (string) table.Rows[0][0];

        var tank = Server.Tanks.Find(t =>
            t.Name.ToLower() == selectedTankName.ToLower());

        ServerSend.SwitchTank(Server.Clients[fromClient], tank, true);
    }

    public static void InstantiateObject(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        var name = packet.ReadString();
        var position = packet.ReadVector3();
        var rotation = packet.ReadQuaternion();

        ServerSend.InstantiateObject(name, position, rotation, fromClient,
            Server.Clients[fromClient].ConnectedRoom);
    }

    public static void ShootBullet(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        var connectedRoom = Server.Clients[fromClient].ConnectedRoom;
        if (connectedRoom is {PlayersLocked: true}) {
            Server.Clients[fromClient].Disconnect("Игрок разблокировал себя на стороне клиента. (Стрельба)");
            return;
        }

        var name = packet.ReadString();
        var particlePrefab = packet.ReadString();
        var position = packet.ReadVector3();
        var rotation = packet.ReadQuaternion();

        var player = Server.Clients[fromClient].Player;
        player?.Shoot(name, particlePrefab, position, rotation);
    }

    public static void LeaveToLobby(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];

        ServerSend.LeaveToLobby(client.Id);
    }

    public static void TakeDamage(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        var hitPlayerId = packet.ReadInt();
        var attacker = Server.Clients[fromClient].Player;
        var hitPlayer = Server.Clients[hitPlayerId].Player;

        if (attacker != null && hitPlayer != null && attacker.Team.Id != hitPlayer.Team.Id) {
            var damage = attacker.SelectedTank.Damage;
            var randomCoof = new Random().Next(-20, 20) * ((float) damage / 100);
            var calculatedDamage = damage + (int) randomCoof;

            if (calculatedDamage == 0)
                calculatedDamage = damage;
            else if (calculatedDamage > hitPlayer.Health) calculatedDamage = hitPlayer.Health;

            attacker.TotalDamage += calculatedDamage;

            if (hitPlayer.Health > 0) ServerSend.ShowDamage(attacker.Id, calculatedDamage, hitPlayer);

            hitPlayer.TakeDamage(calculatedDamage, attacker);
            ServerSend.TakeDamageOtherPlayer(hitPlayer.ConnectedRoom, hitPlayer);
        }
    }

    public static void JoinOrCreateRoom(int fromClient, Packet packet) {
        var packetSender = Server.Clients[fromClient];
        if (!packetSender.IsAuth) packetSender.Disconnect("Игрок не вошел в аккаунт");

        if (Server.Rooms.Count > 0)
            foreach (var room in Server.Rooms)
                if (room.IsOpen) {
                    var client = Server.Clients[fromClient];
                    if (client == null) return;
                    client.JoinRoom(room);

                    if (room.PlayersCount == room.MaxPlayers) {
                        room.IsOpen = false;
                        room.BalanceTeams();
                    }

                    return;
                }

        var newRoom = new Room(Server.Config.GeneralTime, Server.Config.PreparativeTime);
        Server.Clients[fromClient].JoinRoom(newRoom);

        if (newRoom.PlayersCount == newRoom.MaxPlayers) {
            newRoom.IsOpen = false;
            newRoom.BalanceTeams();
        }
    }

    public static void LeaveRoom(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        var playerRoom = Server.Clients[fromClient].ConnectedRoom;
        if (playerRoom is {IsOpen: true}) Server.Clients[fromClient].LeaveRoom();
    }

    public static void CheckAbleToReconnect(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        foreach (var room in Server.Rooms.Where(room => room is {IsOpen: false}))
        foreach (var cachedPlayer in room?.CachedPlayers.Where(cachedPlayer =>
                     cachedPlayer?.Username.ToLower() == Server.Clients[fromClient].Data.Username?.ToLower() &&
                     cachedPlayer.IsAlive && !room.GameEnded)) {
            ServerSend.AbleToReconnect(fromClient);
            Log.Information($"{Server.Clients[fromClient].Data.Username} can reconnect to battle");
        }
    }

    public static void Reconnect(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        foreach (var room in Server.Rooms.Where(room => room is {IsOpen: false})) {
            var cachedPlayer = room?.CachedPlayers.Find(player =>
                player?.Username?.ToLower() == Server.Clients[fromClient].Data.Username?.ToLower());
            if (cachedPlayer == null) return;

            client.JoinRoom(room);
            client.Team = cachedPlayer?.Team;
            client?.Team?.Players.Add(client);
            client.Reconnected = true;
            client.Player = new Player(cachedPlayer, fromClient);
            ServerSend.LoadScene(fromClient, room.Map.Name);
        }
    }

    public static void CancelReconnect(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        foreach (var room in Server.Rooms)
        foreach (var cachedPlayer in room.CachedPlayers)
            if (cachedPlayer?.Username == Server.Clients[fromClient].Data.Username) {
                Log.Information($"{cachedPlayer?.Username} canceled reconnect");
                room.CachedPlayers[room.CachedPlayers.IndexOf(cachedPlayer)] = null;
                return;
            }
    }

    public static void RequestPlayersStats(int fromClient, Packet packet) {
        var room = Server.Clients[fromClient].ConnectedRoom;

        if (room?.Players == null) return;

        ServerSend.SendPlayersStats(room);
    }

    public static void HandleDataRequest(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) {
            client.Disconnect("Игрок не вошел в аккаунт");
            return;
        }

        ServerSend.SendPlayerData(client);
    }

    public static async void AuthById(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        var id = packet.ReadString();
        if (client.IsAuth) return;
        var isAuth = await DatabaseUtils.TryLoginByToken(id, fromClient);

        if (isAuth) {
            var table = await DatabaseUtils.RequestData(
                $"SELECT Count(*) FROM `playerstats` WHERE `nickname` = '{client.Data.Username}'");

            if ((long) table.Rows[0][0] <= 0)
                await DatabaseUtils.ExecuteNonQuery(
                    $"INSERT INTO `playerstats` (`nickname`, `rankID`, `raider`) VALUES ('{client.Data.Username}', 1, 1)");

            client.Data = await DatabaseUtils.GetPlayerData(client);
            ServerSend.SendPlayerData(client);
        }

        ServerSend.LoginResult(fromClient, isAuth, isAuth ? "Авторизация прошла успешно" : "Сессия завершeна");
    }

    public static void SignOut(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        client.IsAuth = false;
        client.Data = default;
        ServerSend.SignOut(fromClient);
    }

    public static void ReceiveMessage(int fromClient, Packet packet) {
        var message = packet.ReadString();
        var client = Server.Clients[fromClient];

        if (!client.IsAuth) {
            client.Disconnect("Игрок не вошел в аккаунт");
            return;
        }

        if (client.ConnectedRoom == null) return;

        ServerSend.SendMessage(MessageType.Player, client.Data.Username, message, client.ConnectedRoom);
    }

    public static void OpenProfile(int fromClient, Packet packet) {
        ServerSend.OpenProfile(fromClient);
    }

    public static void BuyTankRequest(int fromClient, Packet packet) {
        var tankName = packet.ReadString();
        var tank = Server.Tanks.Find(t => string.Equals(t.Name, tankName, StringComparison.CurrentCultureIgnoreCase));
        var client = Server.Clients[fromClient];

        if (tank == null) {
            client.Disconnect("Игрок запросил несуществующий танк");
            return;
        }

        var successful = false;
        string message;

        if (client.Data.Balance >= tank.Cost) {
            try {
                client.Data.Balance -= tank.Cost;
                DatabaseUtils.ExecuteNonQuery(
                    $"UPDATE `playerstats` SET `{tankName.ToLower()}` = '1', `balance` = '{client.Data.Balance}' WHERE `nickname` = '{client.Data.Username}'");
                successful = true;
                message = "Успех";

                ServerSend.SwitchTank(client, tank, true);
            }
            catch (Exception e) {
                client.Data.Balance += tank.Cost;
                Log.Error($"Cannot buy tank '{tankName}'. Error: {e}");
                successful = false;
                message = "Не удалось купить танк. Попробуйте еще раз";
            }
        }
        else {
            message = $"Не хватает {tank.Cost - client.Data.Balance} воксов для покупки этого танка.";
            successful = false;
        }

        ServerSend.SendBoughtTankInfo(fromClient, message, successful);
        ServerSend.SendPlayerData(client);
    }

    public static void RamPlayer(int fromClient, Packet packet) {
        float ramVelocity = packet.ReadFloat();
        int otherPlayerId = packet.ReadInt();
        Client client = Server.Clients[fromClient];
        Client otherClient = Server.Clients[otherPlayerId];
    }
}