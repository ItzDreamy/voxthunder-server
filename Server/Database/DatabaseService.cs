using VoxelTanksServer.Library.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer.Database;

public class DatabaseService : IDatabaseService {
    public DatabaseContext Context =>
        _context ?? throw new InvalidOperationException("DatabaseContext is not initialized");

    private readonly DatabaseContext? _context;

    public DatabaseService() {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var config = deserializer.Deserialize<DatabaseConfig>(File.ReadAllText("Library/Config/databaseCfg.yml"));
        _context = new DatabaseContext(config);
    }
}