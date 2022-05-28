# VoxThunderServer
The game server for VoxThunder

## State:
In development

## Configuration:
- Tune config for ur server. ```Server/Library/Config/config.yml```
- Create database config and customize it. ```Server/Library/Config/databaseCfg.yml```
- Create discord config and customize it. ```Server/Library/Config/DiscordConfig.json```
###### config.yml example:
```
clientVersion: "dev_0.0.1.0091"
maxCredits: 99999999
generalTime: 420000
preparativeTime: 15000
maxPlayersInRoom: 2
afkTime: 900000
maxPlayers: 100
serverPort: 44839
apiPort: 59483
apiMaxConnections: 10000
```
###### databaseCfg.yml example:
```
host: "localhost"
port: "3306"
database: "VoxThunder"
uid: "VoxThunder"
password: "VoxThunder"
```
###### DiscordConfig.json example:
```
{
  "Token": "your discord bot token",
  "Prefix": "!"
}
```
## Start up
- Open terminal
- Go to project directory
- ```dotnet run```
