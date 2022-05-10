using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using VoxelTanksServer.Discord.Services;

namespace VoxelTanksServer.Discord;

public class StartUp {
    public static async Task MainAsync() {
        var builder = new HostBuilder().ConfigureAppConfiguration(configurationBuilder => {
                var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("DiscordConfig.json", false, true).Build();

                configurationBuilder.AddConfiguration(configuration);
            }).ConfigureLogging(loggingBuilder => {
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(LogLevel.Error);
            }).ConfigureDiscordHost((context, config) => {
                config.SocketConfig = new DiscordSocketConfig() {
                    LogLevel = LogSeverity.Error,
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 200,
                    GatewayIntents = GatewayIntents.All
                };

                config.Token = context.Configuration["Token"] ?? string.Empty;
            }).UseCommandService((context, config) => {
                config.CaseSensitiveCommands = false;
                config.LogLevel = LogSeverity.Error;
                config.DefaultRunMode = RunMode.Async;
            }).ConfigureServices((context, services) =>
            {
                services.AddHostedService<CommandHandler>();
                services.AddHostedService<LogHandler>();
            })
            .UseConsoleLifetime();
        var host = builder.Build();
        using (host) {
            await host.RunAsync();
        }
    }
}