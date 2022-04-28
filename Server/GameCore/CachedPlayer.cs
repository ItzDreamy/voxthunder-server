using System.Numerics;

namespace VoxelTanksServer.GameCore;

/// <summary>
/// Класс для хранения данных отключившегося игрока
/// </summary>
public class CachedPlayer
{
    public string? Username;
    public Tank SelectedTank;
    public string TankName;
    public Team? Team;
    public Vector3 Position;
    public Quaternion Rotation;
    public Quaternion BarrelRotation;
    public Quaternion TurretRotation;
    public int Health;
    public int TotalDamage;
    public int Kills;
    public bool CanShoot;
    public bool IsAlive;

    /// <summary>
    /// Создание кеша игрока
    /// </summary>
    /// <param name="player">Отключившийся игрок</param>
    public CachedPlayer(Player player)
    {
        Username = player.Username;
        SelectedTank = player.SelectedTank;
        Team = player.Team;
        Position = player.Position;
        Rotation = player.Rotation;
        BarrelRotation = player.BarrelRotation;
        TurretRotation = player.TurretRotation;
        Health = player.Health;
        TotalDamage = player.TotalDamage;
        Kills = player.Kills;
        CanShoot = player.CanShoot;
        IsAlive = player.IsAlive;
    }
}