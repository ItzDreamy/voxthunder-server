using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VoxelTanksServer.Database;
using VoxelTanksServer.Database.Models;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;
using VoxelTanksServer.Library.Quests;

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
        if (player.Reconnected) player.SendIntoGame(player.Data.Nickname, player.SelectedTank);
    }

    public static void ChangeTank(int fromClient, Packet packet) {
        string tankName = packet.ReadString()!;
        var client = Server.Clients[fromClient];

        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        int ownedValue = 0;
        switch (tankName.ToLower()) {
            case "raider":
                ownedValue = client.Data.Raider;
                break;
            case "mamont":
                ownedValue = client.Data.Mamont;
                break;
            case "berserk":
                ownedValue = client.Data.Berserk;
                break;
        }

        var isOwned = ownedValue > 0;

        var tank = Server.DatabaseService.Context.TanksStats.ToList().Find(t =>
            string.Equals(t.TankName, tankName, StringComparison.CurrentCultureIgnoreCase));
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
            if ((Server.DatabaseService.Context.PlayerStats.ToList()).Any(data =>
                    string.Equals(data.Nickname, username, StringComparison.CurrentCultureIgnoreCase)) == false) {
                Server.DatabaseService.Context.PlayerStats.Add(new PlayerData {
                    Nickname = client.Data.Nickname,
                    RankId = 1,
                    Raider = 1,
                    SelectedTank = "raider"
                });

                Server.DatabaseService.Context.SaveChanges();
            }

            client.Data = (Server.DatabaseService.Context.PlayerStats.ToList()).Find(data =>
                string.Equals(data.Nickname, client.Data.Nickname, StringComparison.CurrentCultureIgnoreCase))!;
            QuestManager.CheckAndUpdateQuests(client);
            ServerSend.LoginResult(fromClient, true, "Успех");
            ServerSend.SendPlayerData(client);
        }
        else {
            ServerSend.LoginResult(fromClient, false, "Неправильный логин или пароль");
        }
    }

    public static async void GetLastSelectedTank(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        var selectedTankName =
            client.Data
                .SelectedTank;
        var tank = (Server.DatabaseService.Context.TanksStats.ToList()).Find(t =>
            string.Equals(t.TankName, selectedTankName, StringComparison.CurrentCultureIgnoreCase));

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
                     cachedPlayer?.Username.ToLower() == Server.Clients[fromClient].Data.Nickname?.ToLower() &&
                     cachedPlayer.IsAlive && !room.GameEnded)) {
            ServerSend.AbleToReconnect(fromClient);
            Log.Information($"{Server.Clients[fromClient].Data.Nickname} can reconnect to battle");
        }
    }

    public static void Reconnect(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        if (!client.IsAuth) client.Disconnect("Игрок не вошел в аккаунт");

        foreach (var room in Server.Rooms.Where(room => room is {IsOpen: false})) {
            var cachedPlayer = room?.CachedPlayers.Find(player =>
                player?.Username?.ToLower() == Server.Clients[fromClient].Data.Nickname?.ToLower());
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
            if (cachedPlayer?.Username == Server.Clients[fromClient].Data.Nickname) {
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

    public static void AuthById(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        var id = packet.ReadString();
        if (client.IsAuth) return;

        var authedClient =
            Server.DatabaseService.Context.AuthData.ToList().Find(data => data.AuthId == id);
        bool isAuth = false;
        if (authedClient != null) {
            var nickname = authedClient.Login;
            Log.Information($"{nickname} успешно зашел в аккаунт");

            var samePlayer = Server.Clients.Values.ToList()
                .Find(player =>
                    player.Data != null &&
                    string.Equals(player.Data.Nickname, nickname, StringComparison.CurrentCultureIgnoreCase));
            samePlayer?.Disconnect("Другой игрок зашел в аккаунт");

            client.Data = new PlayerData();
            client.Data.Nickname = nickname;
            client.IsAuth = true;
            var guid = Guid.NewGuid();
            authedClient.AuthId = guid.ToString();
            Server.DatabaseService.Context.SaveChanges();
            ServerSend.SendAuthId(guid.ToString(), fromClient);
            isAuth = true;
        }

        if (isAuth) {
            if ((Server.DatabaseService.Context.PlayerStats.ToList()).Any(data =>
                    string.Equals(data.Nickname, client.Data.Nickname, StringComparison.CurrentCultureIgnoreCase)) ==
                false) {
                Server.DatabaseService.Context.PlayerStats.Add(new PlayerData {
                    Nickname = client.Data.Nickname,
                    RankId = 1,
                    Raider = 1,
                    SelectedTank = "raider"
                });
            }

            Server.DatabaseService.Context.SaveChanges();

            client.Data = Server.DatabaseService.Context.PlayerStats.ToList().Find(data =>
                string.Equals(data.Nickname, client.Data.Nickname, StringComparison.CurrentCultureIgnoreCase))!;
            QuestManager.CheckAndUpdateQuests(client);
            ServerSend.SendPlayerData(client);
        }

        ServerSend.LoginResult(fromClient, isAuth, isAuth ? "Авторизация прошла успешно" : "Сессия завершeна");
    }

    public static void SignOut(int fromClient, Packet packet) {
        var client = Server.Clients[fromClient];
        client.IsAuth = false;

        var databaseData = (Server.DatabaseService.Context.PlayerStats.ToList())
            .Find(data =>
                string.Equals(data.Nickname, client.Data.Nickname, StringComparison.CurrentCultureIgnoreCase));
        databaseData = (PlayerData) client.Data.Clone();
        Server.DatabaseService.Context.SaveChanges();
        
        client.Reconnected = false;
        client.Player = null;
        client.SpawnPosition = Vector3.Zero;
        client.SpawnRotation = Quaternion.Identity;
        client.SelectedTank = null;
        client.ReadyToSpawn = false;

        client.Data = null;
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

        ServerSend.SendMessage(MessageType.Player, client.Data.Nickname, message, client.ConnectedRoom);
    }

    public static void OpenProfile(int fromClient, Packet packet) {
        ServerSend.OpenProfile(fromClient);
    }

    public static void BuyTankRequest(int fromClient, Packet packet) {
        var tankName = packet.ReadString();
        var tank = Server.DatabaseService.Context.TanksStats.ToList().Find(
            t => string.Equals(t.TankName, tankName, StringComparison.CurrentCultureIgnoreCase));
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

                switch (tankName.ToLower()) {
                    case "raider":
                        client.Data.Raider = 1;
                        break;
                    case "mamont":
                        client.Data.Mamont = 1;
                        break;
                    case "berserk":
                        client.Data.Berserk = 1;
                        break;
                }

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
        float senderVelocity = packet.ReadFloat();
        int otherPlayerId = packet.ReadInt();
        if (senderVelocity < 1.5f)
            return;

        Client client = Server.Clients[fromClient];
        Client otherClient = Server.Clients[otherPlayerId];

        if (client.Player == null) return;

        int damage = (int) (senderVelocity *
            ((float) client.Player.SelectedTank.Weight / 100) / 2);
        client.Player.TotalDamage += damage;
        otherClient.Player.TakeDamage(damage, client.Player);
        ServerSend.TakeDamageOtherPlayer(otherClient.ConnectedRoom, otherClient.Player);
    }
}