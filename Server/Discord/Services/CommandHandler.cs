using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VoxelTanksServer.Discord.Services;

public class CommandHandler : DiscordClientService {
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _provider;

    public CommandHandler(IServiceProvider provider, DiscordSocketClient client, CommandService commandService,
        IConfiguration configuration, ILogger<DiscordClientService> logger) : base(client, logger) {
        _commandService = commandService;
        _configuration = configuration;
        _provider = provider;
        _client = Client;
    }

    private async Task MessageReceived(SocketMessage message) {
        if (message.Author.IsBot || !message.Content.StartsWith(_configuration["Prefix"])) return;

        var context = new SocketCommandContext(_client, message as SocketUserMessage);
        await _commandService.ExecuteAsync(context, 1, _provider);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _client.MessageReceived += MessageReceived;
        _commandService.CommandExecuted += CommandExecuted;
        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
    }

    private async Task CommandExecuted(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result) {
        if (result.IsSuccess) return;

        var response = await context.Channel.SendMessageAsync(result.ErrorReason);

        await Task.Delay(5000);
        await response.DeleteAsync();
    }
}