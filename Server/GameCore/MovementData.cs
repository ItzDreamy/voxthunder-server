using System.Numerics;

namespace VoxelTanksServer.GameCore;

public struct MovementData
{
    public Vector3 Position { get; set; }

    public Vector3 Velocity { get; set; }

    public Vector3 AngularVelocity { get; set; }

    public Quaternion Rotation { get; set; }
        
    public override string ToString()
    {
        return $"Position: {Position} Rotation: {Rotation} Velocity: {Velocity} Angular Velocity: {AngularVelocity}";
    }
}