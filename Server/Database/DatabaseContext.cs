using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MySql.Data.MySqlClient;
using VoxelTanksServer.Database.Models;
using VoxelTanksServer.Library.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer.Database;

public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext> {
    public DatabaseContext CreateDbContext(string[] args) {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var config = deserializer.Deserialize<DatabaseConfig>(File.ReadAllText("Library/Config/databaseCfg.yml"));
        return new DatabaseContext(config);
    }
}

public sealed class DatabaseContext : DbContext {
    public DbSet<PlayerData> playerstats { get; set; }
    public DbSet<AuthData> authdata { get; set; }
    public DbSet<Tank> tanksstats { get; set; }
    public DbSet<OwnedTank> ownedtank { get; set; }

    private readonly DatabaseConfig _config;

    public DatabaseContext(DatabaseConfig config) {
        _config = config;
        Database.EnsureCreated();
        //Database.EnsureDeleted();
        //Database.Migrate();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        MySqlConnectionStringBuilder stringBuilder = new MySqlConnectionStringBuilder() {
            Server = _config.Host,
            UserID = _config.Uid,
            Password = _config.Password,
            Database = _config.Database
        };
        optionsBuilder.UseMySql(
            stringBuilder.ToString(),
            new MySqlServerVersion(new Version(5, 0, 12))
        );
    }
}