using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Serilog;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Discord.Services;

public class LogHandler : DiscordClientService {
    private readonly DiscordSocketClient _client;

    public LogHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger) : base(client, logger) {
        _client = Client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _client.Ready += ClientReady;
    }

    private async Task ClientReady() {
        Log.Information("Bot started");

        while (true) {
            var channel = _client.GetChannel(972929632754102362) as SocketTextChannel;
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle("Статистика сервера").WithColor(Color.Green)
                .AddField("Игроков онлайн", Server.OnlinePlayers)
                .AddField("Количество боёв", Server.Rooms.FindAll(room => !room.IsOpen).Count)
                .WithThumbnailUrl(Client.CurrentUser.GetAvatarUrl());
            channel?.ModifyMessageAsync(973486604897382410, properties => properties.Embed = embedBuilder.Build());

            await Task.Delay(60000);
        }
    }
}