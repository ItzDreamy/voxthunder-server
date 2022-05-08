namespace VoxelTanksServer.GameCore;

public struct Rank {
    public int Id { get; set; }
    public string Name { get; set; }
    public int RequiredExp { get; set; }
    public int Reward { get; set; }

    public override string ToString() {
        return $"Id: {Id} Name: {Name} RequiredExp: {RequiredExp} Reward: {Reward}";
    }
}