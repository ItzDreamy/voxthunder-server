using System.Drawing;
using System.Numerics;
using Serilog;
using VoxelTanksServer.Library;
using VoxelTanksServer.Library.LevelingSystem;
using VoxelTanksServer.Library.Quests;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public class Player {
    public int TakenDamage;
    public Team? Team;
    public int TotalDamage;
    public int Id { get; }
    public string? Username { get; }
    public Tank SelectedTank { get; }
    public Room? ConnectedRoom { get; }
    public MovementData Movement { get; set; }
    public Quaternion BarrelRotation { get; private set; }
    public Quaternion TurretRotation { get; private set; }
    public int Health { get; private set; }
    public int Kills { get; private set; }
    public bool CanShoot { get; private set; }
    public bool IsAlive { get; private set; }
    public DateTime LastShootedTime { get; set; }

    public Player(int id, string? username, Vector3 spawnPosition, Quaternion rotation, Tank tank,
        Room? room) {
        Id = id;
        Username = username;
        SelectedTank = tank;
        var movement = new MovementData();
        movement.Position = spawnPosition;
        movement.Rotation = rotation;
        Movement = movement;
        IsAlive = true;
        ConnectedRoom = room;

        if (tank == null) {
            Server.Clients[Id].Disconnect("Неизвестный танк");
            return;
        }

        Health = tank.MaxHealth;

        ConnectedRoom.CachedPlayers.Add(CachePlayer());
    }

    public Player(CachedPlayer cachedPlayer, int id) {
        Id = id;
        Username = cachedPlayer.Username;
        Team = cachedPlayer.Team;
        Movement = cachedPlayer.Movement;
        BarrelRotation = cachedPlayer.BarrelRotation;
        TurretRotation = cachedPlayer.TurretRotation;
        CanShoot = cachedPlayer.CanShoot;
        IsAlive = cachedPlayer.IsAlive;
        Health = cachedPlayer.Health;
        TotalDamage = cachedPlayer.TotalDamage;
        Kills = cachedPlayer.Kills;
        ConnectedRoom = Server.Clients[Id].ConnectedRoom;
        LastShootedTime = cachedPlayer.LastShootedTime;

        SelectedTank = cachedPlayer.SelectedTank;

        ServerSend.PlayerReconnected(Username, ConnectedRoom);

        ConnectedRoom.CachedPlayers.Remove(cachedPlayer);
        ConnectedRoom.CachedPlayers.Add(CachePlayer());
    }

    public void Move(Vector3 velocity, Quaternion rotation, Quaternion barrelRotation, float speed, bool isForward) {
        if (!IsAlive || CheckSpeedHack(speed,
                isForward ? SelectedTank.MaxSpeed : SelectedTank.MaxBackSpeed)) return;

        BarrelRotation = barrelRotation;
    }

    private bool CheckSpeedHack(float speed, float maxSpeed) {
        if (speed < maxSpeed) return false;
        return false;
    }

    public void RotateTurret(Quaternion turretRotation, Quaternion barrelRotation) {
        if (IsAlive) {
            TurretRotation = turretRotation;
            BarrelRotation = barrelRotation;
        }

        ServerSend.RotateTurret(this);
    }

    public void TakeDamage(int damage, Player enemy) {
        if (Health <= 0) return;

        Health -= damage;

        if (Health <= 0) {
            Health = 0;
            Die(enemy);
        }

        TakenDamage += damage;

        ServerSend.TakeDamage(Id, SelectedTank.MaxHealth, Health);
    }

    private void Die(Player enemy) {
        enemy.Kills++;

        IsAlive = false;

        ServerSend.PlayerDead(Id);

        foreach (var team in ConnectedRoom.Teams)
            ServerSend.ShowKillFeed(team, team == Team ? Color.Red : Color.Lime, enemy.Username, Username,
                enemy.SelectedTank.Name,
                SelectedTank.Name);

        if (Team.PlayersDeadCheck() && !ConnectedRoom.GameEnded) {
            ConnectedRoom.GameEnded = true;

            Task.Run(async () => {
                ServerSend.SendPlayersStats(ConnectedRoom);
                await Task.Delay(3000);

                foreach (var team in ConnectedRoom.Teams) {
                    foreach (var client in team.Players)
                        client.Player.UpdatePlayerStats(team != Team ? GameResults.Win : GameResults.Lose);

                    ServerSend.EndGame(team, team != Team ? GameResults.Win : GameResults.Lose);
                }

                foreach (var player in ConnectedRoom.Players.Values) player?.LeaveRoom();
            });
        }
    }

    public void Shoot(string? bulletPrefab, string? particlePrefab, Vector3 position, Quaternion rotation) {
        if (!((float) (DateTime.Now - LastShootedTime).TotalSeconds >= SelectedTank.Cooldown) || !IsAlive)
            return;

        ServerSend.InstantiateObject(bulletPrefab, position, rotation, Id, ConnectedRoom);
        ServerSend.InstantiateObject(particlePrefab, position, rotation, Id, ConnectedRoom);
        LastShootedTime = DateTime.Now;
    }

    public CachedPlayer? CachePlayer() {
        return new CachedPlayer(this);
    }

    public async void UpdatePlayerStats(GameResults results) {
        try {
            var client = Server.Clients[Id];

            UpdateBattlesCount(results, client);
            UpdateQuestsData(client, results);
            UpdateRank(client, results);
            UpdateBattleStats(client);
            UpdateBalance(client, results);
            
            ServerSend.SendPlayerData(client);
            await DatabaseUtils.UpdatePlayerData(client.Data);
        }
        catch (Exception exception) {
            Log.Error(exception.ToString());
        }
    }

    private void UpdateQuestsData(Client client, GameResults results) {
        var quests = client.Data.QuestsData.Quests;
        for (int i = 0; i < quests.Count; i++) {
            if (quests[i].Completed)
                continue;

            Quest quest = quests[i];
            switch (quest.Type) {
                case QuestType.Wins:
                    quest.Progress += results == GameResults.Win ? 1 : 0;
                    break;
                case QuestType.Damage:
                    quest.Progress += TotalDamage;
                    break;
                case QuestType.Kills:
                    quest.Progress += Kills;
                    break;
            }

            if (quest.Completed) {
                Log.Debug($"{Username} completed quest! Quest type: {quest.Type.ToString()}");
                RewardPlayer(quest, client);
            }

            quests[i] = quest;
        }

        client.Data.QuestsData.Quests = quests;
        QuestManager.UpdateQuests(client.Data.QuestsData,
            Path.Combine("PlayersData", "Quests", $"{client.Data.Username}.json"));
    }

    private void RewardPlayer(Quest quest, Client client) {
        client.Data.Experience += quest.Reward.Experience;
        client.Data.Balance += quest.Reward.Credits;
        client.Data.Balance = Math.Clamp(client.Data.Balance, 0, Server.Config.MaxCredits);
    }

    private void UpdateBattlesCount(GameResults results, Client client) {
        client.Data.Battles++;

        switch (results) {
            case GameResults.Win:
                client.Data.Wins++;
                break;
            case GameResults.Lose:
                client.Data.Loses++;
                break;
            case GameResults.Draw:
                client.Data.Draws++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(results), results, null);
        }
    }

    private void UpdateRank(Client client, GameResults results) {
        var collectedExperience =
            (int) (TotalDamage * (1 + Kills * 0.25) * (1 + (results == GameResults.Win ? 0.5f : 0)));
        client.Data.Experience += collectedExperience;

        if (Leveling.CheckRankUp(client, out var nextRank)) {
            client.Data.Balance += nextRank.Reward;
            client.Data.Rank = nextRank;
        }
    }

    private void UpdateBalance(Client client, GameResults results) {
        var collectedCredits =
            (int) (TotalDamage * 10 * (Kills + 1) * (1 + (results == GameResults.Win ? 1 : 0)) -
                   TakenDamage * 2.5f * (1 + (IsAlive ? 1 : 0)));
        client.Data.Balance += collectedCredits;
        client.Data.Balance = Math.Clamp(client.Data.Balance, 0, Server.Config.MaxCredits);
    }

    private void UpdateBattleStats(Client client) {
        client.Data.Damage += TotalDamage;
        client.Data.Kills += Kills;
        client.Data.AvgDamage = client.Data.Damage / client.Data.Battles;
        client.Data.AvgKills = client.Data.Kills / client.Data.Battles;
        client.Data.AvgExperience = client.Data.Experience / client.Data.Battles;
        client.Data.WinRate = (client.Data.Wins + 0.5f * client.Data.Draws) / client.Data.Battles * 100f;
        client.Data.WinRate = (float) Math.Round(Math.Clamp(client.Data.WinRate, 0f, 100f), 1,
            MidpointRounding.AwayFromZero);
    }
}