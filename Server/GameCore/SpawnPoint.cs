using System.Numerics;

namespace VoxelTanksServer.GameCore;

public class SpawnPoint : ICloneable {
    public bool IsOpen = true;

    public SpawnPoint(Vector3 position) {
        Rotation = Quaternion.Identity;
        Position = position;
    }

    public SpawnPoint(Vector3 position, Quaternion rotation) {
        Rotation = rotation;
        Position = position;
    }

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }

    public object Clone() {
        return new SpawnPoint(Position, Rotation);
    }
}