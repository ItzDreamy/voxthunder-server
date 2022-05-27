using System.Numerics;
using VoxelTanksServer.Database.Models;

namespace VoxelTanksServer.GameCore;

public class CachedPlayer {
    public Quaternion BarrelRotation;
    public readonly bool CanShoot;
    public readonly int Health;
    public readonly bool IsAlive;
    public readonly int Kills;
    public MovementData Movement;
    public readonly Tank SelectedTank;
    public readonly Team? Team;
    public readonly int TotalDamage;
    public Quaternion TurretRotation;
    public readonly string? Username;
    public DateTime LastShootedTime;

    public CachedPlayer(Player player) {
        Username = player.Username;
        SelectedTank = player.SelectedTank;
        Team = player.Team;
        Movement = player.Movement;
        BarrelRotation = player.BarrelRotation;
        TurretRotation = player.TurretRotation;
        Health = player.Health;
        TotalDamage = player.TotalDamage;
        Kills = player.Kills;
        CanShoot = player.CanShoot;
        IsAlive = player.IsAlive;
        LastShootedTime = player.LastShootedTime;
    }
}