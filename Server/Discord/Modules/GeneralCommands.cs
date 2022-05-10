using Discord;
using Discord.Commands;
using Discord.WebSocket;
using VoxelTanksServer.Protocol;

namespace VoxelTanksServer.Discord.Modules;

public class GeneralCommands : ModuleBase<SocketCommandContext> {
    [Command("статистика"), Alias("stats")]
    private async Task SendServerStats() {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithTitle("Статистика сервера").WithColor(Color.Green)
            .AddField("Игроков онлайн", Server.OnlinePlayers)
            .AddField("Колличество боёв", Server.Rooms.FindAll(room => !room.IsOpen).Count)
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        await ReplyAsync(embed: embedBuilder.Build());
    }
}