using System.Drawing;
using System.Numerics;
using Serilog;
using VoxelTanksServer.Library;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.GameCore;

public class Player
{
    public Team? Team;
    public int TotalDamage;
    public int TakenDamage;
    public int Id { get; private set; }
    public string? Username { get; private set; }
    public Tank SelectedTank { get; private set; }
    public Room? ConnectedRoom { get; private set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; private set; }
    public Quaternion Rotation { get; set; }
    public Quaternion BarrelRotation { get; private set; }
    public Quaternion TurretRotation { get; private set; }
    public int Health { get; private set; }
    public int Kills { get; private set; }
    public bool CanShoot { get; private set; }
    public bool IsAlive { get; private set; }

    public Player(int id, string? username, Vector3 spawnPosition, Quaternion rotation, Tank tank,
        Room? room)
    {
        Id = id;
        Username = username;
        Position = spawnPosition;
        Rotation = rotation;
        SelectedTank = tank;
        IsAlive = true;
        ConnectedRoom = room;

        if (tank == null)
        {
            Server.Clients[Id].Disconnect("Неизвестный танк");
            return;
        }

        Health = tank.MaxHealth;

        ConnectedRoom.CachedPlayers.Add(CachePlayer());

        Task.Run(async () =>
        {
            await Task.Delay((int) (SelectedTank.Cooldown * 1000));
            CanShoot = true;
            return Task.CompletedTask;
        });
    }

    public Player(CachedPlayer cachedPlayer, int id)
    {
        Id = id;
        Username = cachedPlayer.Username;
        Team = cachedPlayer.Team;

        Position = cachedPlayer.Position;
        Rotation = cachedPlayer.Rotation;
        BarrelRotation = cachedPlayer.BarrelRotation;
        TurretRotation = cachedPlayer.TurretRotation;
        CanShoot = cachedPlayer.CanShoot;
        IsAlive = cachedPlayer.IsAlive;
        Health = cachedPlayer.Health;
        TotalDamage = cachedPlayer.TotalDamage;
        Kills = cachedPlayer.Kills;
        ConnectedRoom = Server.Clients[Id].ConnectedRoom;

        SelectedTank = cachedPlayer.SelectedTank;

        ServerSend.PlayerReconnected(Username, ConnectedRoom);

        ConnectedRoom.CachedPlayers.Remove(cachedPlayer);
        ConnectedRoom.CachedPlayers.Add(CachePlayer());

        if (!CanShoot && IsAlive)
        {
            Task.Run(async () =>
            {
                await Task.Delay((int) (SelectedTank.Cooldown * 1000));
                CanShoot = true;
                return Task.CompletedTask;
            });
        }
    }

    public void Move(Vector3 velocity, Quaternion rotation, Quaternion barrelRotation, float speed, bool isForward)
    {
        if (!IsAlive || CheckSpeedHack(speed,
                isForward ? SelectedTank.MaxSpeed : SelectedTank.MaxBackSpeed)) return;

        Rotation = rotation;
        Velocity = velocity;
        BarrelRotation = barrelRotation;
    }

    private bool CheckSpeedHack(float speed, float maxSpeed)
    {
        if (speed < maxSpeed) return false;
        return false;
    }

    public void RotateTurret(Quaternion turretRotation, Quaternion barrelRotation)
    {
        if (IsAlive)
        {
            TurretRotation = turretRotation;
            BarrelRotation = barrelRotation;
        }

        ServerSend.RotateTurret(this);
    }

    public void TakeDamage(int damage, Player enemy)
    {
        if (Health <= 0)
        {
            return;
        }

        Health -= damage;

        if (Health <= 0)
        {
            Health = 0;
            Die(enemy);
        }

        TakenDamage += damage;

        ServerSend.TakeDamage(Id, SelectedTank.MaxHealth, Health);
    }

    private void Die(Player enemy)
    {
        enemy.Kills++;

        IsAlive = false;

        ServerSend.PlayerDead(Id);

        foreach (var team in ConnectedRoom.Teams)
        {
            ServerSend.ShowKillFeed(team, team == Team ? Color.Red : Color.Lime, enemy.Username, Username,
                enemy.SelectedTank.Name,
                SelectedTank.Name);
        }

        if (Team.PlayersDeadCheck() && !ConnectedRoom.GameEnded)
        {
            ConnectedRoom.GameEnded = true;

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                ServerSend.SendPlayersStats(ConnectedRoom);

                foreach (var team in ConnectedRoom.Teams)
                {
                    foreach (var client in team.Players)
                    {
                        client.Player.UpdatePlayerStats(team != Team ? GameResults.Win : GameResults.Lose);
                    }

                    ServerSend.EndGame(team, team != Team ? GameResults.Win : GameResults.Lose);
                }

                foreach (var player in ConnectedRoom.Players.Values)
                {
                    player?.LeaveRoom();
                }
            });
        }
    }

    public void Shoot(string? bulletPrefab, string? particlePrefab, Vector3 position, Quaternion rotation)
    {
        if (!CanShoot || !IsAlive)
            return;

        CanShoot = false;
        ServerSend.InstantiateObject(bulletPrefab, position, rotation, Id, ConnectedRoom);
        ServerSend.InstantiateObject(particlePrefab, position, rotation, Id, ConnectedRoom);

        Task.Run(async () =>
        {
            await Task.Delay((int) (SelectedTank.Cooldown * 1000));
            CanShoot = true;
            return Task.CompletedTask;
        });
    }

    public CachedPlayer? CachePlayer()
    {
        return new CachedPlayer(this);
    }

    public async void UpdatePlayerStats(GameResults results)
    {
        try
        {
            var client = Server.Clients[Id];
            client.Stats.Battles++;

            switch (results)
            {
                case GameResults.Win:
                    client.Stats.Wins++;
                    break;
                case GameResults.Lose:
                    client.Stats.Loses++;
                    break;
                case GameResults.Draw:
                    client.Stats.Draws++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(results), results, null);
            }

            int collectedExperience =
                (int) (TotalDamage * (1 + (Kills * 0.25)) * (1 + (results == GameResults.Win ? 0.5f : 0))); 
            client.Stats.Experience += collectedExperience;
            
            client.Stats.Damage += TotalDamage;
            client.Stats.Kills += Kills;
            client.Stats.AvgDamage = client.Stats.Damage / client.Stats.Battles;
            client.Stats.AvgKills = client.Stats.Kills / client.Stats.Battles;
            client.Stats.AvgExperience = client.Stats.Experience / client.Stats.Battles;
            client.Stats.WinRate = ((client.Stats.Wins + 0.5f * client.Stats.Draws) / client.Stats.Battles) * 100f;
            client.Stats.WinRate = (float) Math.Round(Math.Clamp(client.Stats.WinRate, 0f, 100f), 1, MidpointRounding.AwayFromZero);

            int collectedCredits =
                (int) (TotalDamage * 10 * (Kills + 1) * (1 + (results == GameResults.Win ? 1 : 0)) -
                       TakenDamage * 2.5f * (1 + (IsAlive ? 1 : 0)));
            client.Stats.Balance += collectedCredits;
            client.Stats.Balance = Math.Clamp(client.Stats.Balance, 0, Server.Config.MaxCredits);

            Rank nextRank = RankSystemUtils.GetRank(client.Stats.Rank.Id + 1);
            if (client.Stats.Experience >= nextRank.RequiredExp)
            {
                client.Stats.Balance += nextRank.Reward;
                client.Stats.Rank = nextRank;
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception.ToString());
        }
    }
}