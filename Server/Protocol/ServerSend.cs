using System.Drawing;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VoxelTanksServer.Database.Models;
using VoxelTanksServer.GameCore;
using VoxelTanksServer.Library;
using VoxelTanksServer.Library.LevelingSystem;

namespace VoxelTanksServer.Protocol;

public static class ServerSend {
    private static void SendTcpData(int toClient, Packet packet) {
        packet.WriteLength();
        Server.Clients[toClient].Tcp.SendData(packet);
    }

    private static void SendTcpDataToAll(Packet packet) {
        packet.WriteLength();
        for (var i = 1; i < Server.MaxPlayers; i++) Server.Clients[i].Tcp.SendData(packet);
    }

    public static void SendTcpDataToRoom(Room? room, Packet packet) {
        packet.WriteLength();

        if (room != null)
            foreach (var player in room.Players.Values)
                player.Tcp.SendData(packet);
    }

    public static void SendTcpDataToTeam(Team? team, Packet packet) {
        packet.WriteLength();

        foreach (var client in team.Players) client.Tcp.SendData(packet);
    }

    public static void SendTcpDataToRoom(Room? room, int exceptId, Packet packet) {
        packet.WriteLength();

        foreach (var player in room.Players.Values)
            if (player.Id != exceptId)
                player.Tcp.SendData(packet);
    }

    public static void SendTcpDataToTeam(Team? team, int exceptId, Packet packet) {
        packet.WriteLength();

        foreach (var client in team.Players)
            if (client.Id != exceptId)
                client.Tcp.SendData(packet);
    }

    private static void SendTcpDataToAll(int exceptClient, Packet packet) {
        packet.WriteLength();
        for (var i = 1; i < Server.MaxPlayers; i++)
            if (i != exceptClient)
                Server.Clients[i].Tcp.SendData(packet);
    }

    #region Packets

    public static void Welcome(int toClient, string? message) {
        using (Packet packet = new((int) ServerPackets.Welcome)) {
            packet.Write(message);
            packet.Write(toClient);
            packet.Write(Server.Config.ClientVersion);

            SendTcpData(toClient, packet);
        }
    }

    public static void SpawnPlayer(int toClient, Player? player) {
        using (Packet packet = new((int) ServerPackets.SpawnPlayer)) {
            packet.Write(player.ConnectedRoom.PlayersCount);
            packet.Write(player.Id);
            packet.Write(player.Team.Id);
            packet.Write(player.Username);
            packet.Write(player.Movement.Position);
            packet.Write(player.Movement.Rotation);
            packet.Write(player.TurretRotation);
            packet.Write(player.BarrelRotation);
            packet.Write(player.SelectedTank.TankName);
            packet.Write(player.ConnectedRoom.PlayersLocked);

            SendTcpData(toClient, packet);

            InitializeTankStats(toClient, player);
        }
    }

    public static void InitializeTankStats(int toClient, Player? player) {
        using (var packet = new Packet((int) ServerPackets.InitializeTankStats)) {
            packet.Write(player.Id);
            packet.Write(!player.CanShoot);
            packet.Write(player.SelectedTank.Cooldown);
            packet.Write(player.Health);
            packet.Write(player.SelectedTank.Health);
            packet.Write(player.SelectedTank.MaxSpeed);
            packet.Write(player.SelectedTank.BackSpeed);
            packet.Write(player.SelectedTank.AccelerationSpeed);
            packet.Write(player.SelectedTank.BackAccelerationSpeed);
            packet.Write(player.SelectedTank.TankRotateSpeed);
            packet.Write(player.SelectedTank.TowerRotateSpeed);
            packet.Write(player.SelectedTank.AngleUp);
            packet.Write(player.SelectedTank.AngleDown);

            SendTcpData(toClient, packet);
        }
    }

    public static void SwitchTank(Client client, Tank tank, bool isOwned) {
        using (var packet = new Packet((int) ServerPackets.SwitchTank)) {
            if (isOwned) {
                client.SelectedTank = tank;
                client.Data.SelectedTank = tank.TankName;
            }

            var tanks = Server.DatabaseService.Context.tanksstats.ToList();

            var topHealth = tanks.Max(t => t.Health);
            var topDamage = tanks.Max(t => t.Damage);
            var topSpeed = tanks.Max(t => t.MaxSpeed);

            packet.Write(isOwned);
            packet.Write(tank.TankName);
            packet.Write(tank.Cost);
            packet.Write(tank.Health);
            packet.Write(topHealth);
            packet.Write(tank.Damage);
            packet.Write(topDamage);
            packet.Write(tank.MaxSpeed);
            packet.Write(topSpeed);

            SendTcpData(client.Id, packet);
        }
    }

    public static void RotateTurret(Player player) {
        using (Packet packet = new((int) ServerPackets.RotateTurret)) {
            packet.Write(player.Id);
            packet.Write(player.TurretRotation);
            packet.Write(player.BarrelRotation);

            SendTcpDataToRoom(player.ConnectedRoom, player.Id, packet);
        }
    }

    public static void LoginResult(int toClient, bool result, string? message) {
        using (Packet packet = new((int) ServerPackets.LoginResult)) {
            packet.Write(result);
            packet.Write(message);

            SendTcpData(toClient, packet);
        }
    }

    public static void InstantiateObject(string? name, Vector3 position, Quaternion rotation, int fromClient,
        Room? room) {
        using (Packet packet = new((int) ServerPackets.InstantiateObject)) {
            packet.Write(name);
            packet.Write(position);
            packet.Write(rotation);
            packet.Write(fromClient);

            SendTcpDataToRoom(room, packet);
        }
    }

    public static void LoadScene(Room? room, string? sceneName) {
        using (Packet packet = new((int) ServerPackets.LoadGame)) {
            packet.Write(sceneName);
            SendTcpDataToRoom(room, packet);
        }
    }

    public static void LoadScene(int toClient, string? sceneName) {
        using (Packet packet = new((int) ServerPackets.LoadGame)) {
            packet.Write(sceneName);
            SendTcpData(toClient, packet);
        }
    }

    public static void PlayerDisconnected(int playerId, Room? room) {
        using (Packet packet = new((int) ServerPackets.PlayerDisconnected)) {
            packet.Write(playerId);
            SendTcpDataToRoom(room, packet);
        }
    }

    public static void PlayerReconnected(string username, Room? room) {
        using (Packet packet = new((int) ServerPackets.PlayerReconnected)) {
            packet.Write(username);

            SendTcpDataToRoom(room, packet);
        }
    }

    public static void TakeDamage(int playerId, int maxHealth, int currentHealth) {
        using (Packet packet = new((int) ServerPackets.TakeDamage)) {
            packet.Write(playerId);
            packet.Write(maxHealth);
            packet.Write(currentHealth);

            SendTcpData(playerId, packet);
        }
    }

    public static void PlayerDead(int playerId) {
        using (Packet packet = new((int) ServerPackets.PlayerDead)) {
            packet.Write(playerId);

            SendTcpDataToRoom(Server.Clients[playerId].ConnectedRoom, packet);
        }
    }

    public static void AbleToReconnect(int toClient) {
        using (Packet packet = new((int) ServerPackets.AbleToReconnect)) {
            SendTcpData(toClient, packet);
        }
    }

    public static void ShowDamage(int toClient, int damage, Player player) {
        using (var packet = new Packet((int) ServerPackets.ShowDamage)) {
            packet.Write(player.Id);
            packet.Write(damage);

            SendTcpData(toClient, packet);
        }
    }

    public static void TakeDamageOtherPlayer(Room room, Player player) {
        using (var packet = new Packet((int) ServerPackets.TakeDamageOtherPlayer)) {
            packet.Write(player.Id);
            packet.Write(player.SelectedTank.Health);
            packet.Write(player.Health);

            SendTcpDataToRoom(room, packet);
        }
    }

    public static void ShowKillFeed(Team team, Color color, string killerUsername, string deadUsername,
        string killerTank, string deadTank) {
        using (var packet = new Packet((int) ServerPackets.ShowKillFeed)) {
            packet.Write(killerUsername);
            packet.Write(deadUsername);
            packet.Write(killerTank);
            packet.Write(deadTank);
            packet.Write(color);

            SendTcpDataToTeam(team, packet);
        }
    }

    public static void ShowPlayersCountInRoom(Room room) {
        using (var packet = new Packet((int) ServerPackets.ShowPlayersCountInRoom)) {
            packet.Write(room.PlayersCount);
            packet.Write(room.MaxPlayers);
            SendTcpDataToRoom(room, packet);
        }
    }

    public static void SendPlayersStats(Room room) {
        using (var packet = new Packet((int) ServerPackets.PlayersStats)) {
            packet.Write(room.Players.Values.ToList().Where(client => client.Player != null)
                .Select(client => client.Player).ToList());
            SendTcpDataToRoom(room, packet);
        }
    }

    public static void EndGame(Team team, GameResults result) {
        using (var packet = new Packet((int) ServerPackets.EndGame)) {
            packet.Write((int) result);
            SendTcpDataToTeam(team, packet);
        }
    }

    public static void LeaveToLobby(int toClient) {
        using (var packet = new Packet((int) ServerPackets.LeaveToLobby)) {
            packet.Write("Lobby");
            SendTcpData(toClient, packet);
        }
    }

    public static void SendTimer(Room room, int time, bool isGeneral) {
        using (var packet = new Packet((int) ServerPackets.Timer)) {
            packet.Write(time);
            packet.Write(isGeneral);
            SendTcpDataToRoom(room, packet);
        }
    }

    public static void UnlockPlayers(Room room) {
        using (var packet = new Packet((int) ServerPackets.UnlockPlayers)) {
            SendTcpDataToRoom(room, packet);
        }
    }

    public static void SendMovementData(MovementData movement, Room room, int id) {
        using (var packet = new Packet((int) ServerPackets.SendMovement)) {
            packet.Write(id);
            packet.Write(movement);

            SendTcpDataToRoom(room, packet);
        }
    }

    public static void SendPlayerData(Client toClient) {
        using (var packet = new Packet((int) ServerPackets.PlayerData)) {
            var data = toClient.Data;
            var rank = toClient.Data.Rank;
            packet.Write(data.Nickname);

            packet.Write(rank.Id);
            packet.Write(rank.Name);
            packet.Write(rank.Reward);
            packet.Write(rank.RequiredExp);

            packet.Write(data.Battles);
            packet.Write(data.Damage);
            packet.Write(data.Kills);
            packet.Write(data.Wins);
            packet.Write(data.Loses);
            packet.Write(data.Draws);
            packet.Write(data.WinRate);
            packet.Write(data.AvgDamage);
            packet.Write(data.AvgExp);
            packet.Write(data.Balance);
            packet.Write(data.Exp);
            packet.Write(Leveling.MaxRank.Id != rank.Id ? Leveling.GetRank(rank.Id + 1).RequiredExp : rank.RequiredExp);
            foreach (var quest in data.QuestsData.Quests) {
                packet.Write(quest.Type.ToString());
                packet.Write(quest.Description);
                var timeToUpdate = data.QuestsData.GeneratedDate.AddDays(1) - DateTime.Now;
                packet.Write($"{timeToUpdate.Hours}H {timeToUpdate.Minutes}M");
                packet.Write(quest.Require);
                packet.Write(quest.Progress);
                packet.Write(quest.Reward.Credits);
                packet.Write(quest.Reward.Experience);
            }

            SendTcpData(toClient.Id, packet);
        }
    }

    public static void SendAuthId(string id, int toClient) {
        using (var packet = new Packet((int) ServerPackets.AuthId)) {
            packet.Write(id);

            SendTcpData(toClient, packet);
        }
    }

    public static void SignOut(int toClient) {
        using (var packet = new Packet((int) ServerPackets.SignOut)) {
            SendTcpData(toClient, packet);
        }
    }

    public static void SendMessage(MessageType player, string author, string? message, Room room) {
        using (var packet = new Packet((int) ServerPackets.SendMessage)) {
            packet.Write((int) player);
            packet.Write(author);
            packet.Write(message);
            SendTcpDataToRoom(room, packet);
        }
    }

    public static void OpenProfile(int toClient) {
        using (var packet = new Packet((int) ServerPackets.OpenProfile)) {
            SendTcpData(toClient, packet);
        }
    }

    public static void SendBoughtTankInfo(int toClient, string message, bool isSuccessful) {
        using (var packet = new Packet((int) ServerPackets.BoughtTankInfo)) {
            packet.Write(isSuccessful);
            packet.Write(message);

            SendTcpData(toClient, packet);
        }
    }

    public static void SendLastGameStats(int toClient, GameResults results, int exp, int credits, int kills) {
        using (Packet packet = new Packet((int) ServerPackets.LastGameStats)) {
            packet.Write((int) results);
            packet.Write(credits);
            packet.Write(exp);
            packet.Write(kills);
            
            SendTcpData(toClient, packet);
        }
    }

    public static void SendQuestsTime(TimeSpan time, int toClient) {
        using (Packet packet = new Packet((int) ServerPackets.QuestsTime)) {
            packet.Write($"{time.Hours}H {time.Minutes}M");
            SendTcpData(toClient, packet);
        }
    }

    #endregion
}