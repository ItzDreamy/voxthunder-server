using System.Numerics;

namespace VoxelTanksServer.GameCore;

/// <summary>
///     Класс для хранения данных отключившегося игрока
/// </summary>
public class CachedPlayer {
    public Quaternion BarrelRotation;
    public bool CanShoot;
    public int Health;
    public bool IsAlive;
    public int Kills;
    public Vector3 Position;
    public Quaternion Rotation;
    public Tank SelectedTank;
    public string TankName;
    public Team? Team;
    public int TotalDamage;
    public Quaternion TurretRotation;
    public string? Username;

    /// <summary>
    ///     Создание кеша игрока
    /// </summary>
    /// <param name="player">Отключившийся игрок</param>
    public CachedPlayer(Player player) {
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