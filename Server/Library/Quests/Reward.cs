namespace VoxelTanksServer.Library.Quests;

public struct Reward {
    public int Experience;
    public int Credits;

    public override string ToString() {
        return $"Experience: {Experience} Credits: {Credits}";
    }
}