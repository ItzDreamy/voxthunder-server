using System.ComponentModel.DataAnnotations;

namespace VoxelTanksServer.Database.Models;

public class Tank {
    [Key] public int Id { get; set; }
    public string TankName { get; set; }
    public int Weight { get; set; }
    public int Damage { get; set; }
    public int Health { get; set; }
    public float TowerRotateSpeed { get; set; }
    public float TankRotateSpeed { get; set; }
    public float AngleUp { get; set; }
    public float AngleDown { get; set; }
    public float MaxSpeed { get; set; }
    public float AccelerationSpeed { get; set; }
    public float BackSpeed { get; set; }
    public float BackAccelerationSpeed { get; set; }
    public float Cooldown { get; set; }
    public int Cost { get; set; }

    public override string ToString() {
        return $"Name: {TankName}";
    }
}