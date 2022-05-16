using MySql.Data.MySqlClient;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VoxelTanksServer.DB;

public class Database {
    private readonly MySqlConnection _connection;

    public Database() {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var config = deserializer.Deserialize<DatabaseConfig>(File.ReadAllText("Library/Config/databaseCfg.yml"));
        var host = config.Host;
        var port = config.Port;
        var database = config.Database;
        var uid = config.Uid;
        var password = config.Password;
        _connection =
            new MySqlConnection(
                $"Server={host};Port={port};Database={database};Uid={uid};Pwd={password};Charset=utf8");
    }

    public MySqlConnection GetConnection() {
        return _connection;
    }
}

public class DatabaseConfig {
    public string Host;
    public string Port;
    public string Database;
    public string Uid;
    public string Password;
}