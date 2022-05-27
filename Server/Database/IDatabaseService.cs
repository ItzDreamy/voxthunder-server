namespace VoxelTanksServer.Database; 

public interface IDatabaseService {
    public DatabaseContext Context { get; }
}