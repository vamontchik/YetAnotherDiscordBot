using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Interactions;
using DiscordBot.Handler;
using DiscordBot.Modules;
using DiscordBot.Modules.Audio;
using DiscordBot.Modules.RockPaperScissors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordBot;

internal static class Program
{
    public static async Task Main() => await MainAsync();

    private static async Task MainAsync()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddYamlFile("config.yml")
            .Build();

        using var host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) => services
                .AddSingleton(config)
                .AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.All,
                    AlwaysDownloadUsers = true, // TODO: is this necessary ?
                    MessageCacheSize = 100,
                    LogLevel = LogSeverity.Info
                }))
                .AddSingleton(serviceProvider =>
                    new InteractionService(
                        serviceProvider.GetRequiredService<DiscordSocketClient>(),
                        new InteractionServiceConfig
                        {
                            LogLevel = LogSeverity.Info,
                            DefaultRunMode = Discord.Interactions.RunMode.Async
                        }))
                .AddSingleton<InteractionHandler>()
                .AddSingleton(_ => new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Info,
                    DefaultRunMode = Discord.Commands.RunMode.Async
                }))
                .AddSingleton<PrefixHandler>()
                .AddSingleton<StatsManager>()
                .AddSingleton<AudioService>()
                .AddSingleton<AudioStore>()
                .AddSingleton<AudioConnector>()
                .AddSingleton<AudioDisposer>()
            )
            .Build();

        await RunAsync(host);
    }

    private static async Task RunAsync(IHost host)
    {
        using var serviceScope = host.Services.CreateScope();
        var provider = serviceScope.ServiceProvider;

        var client = provider.GetRequiredService<DiscordSocketClient>();

        var slashCommands = provider.GetRequiredService<InteractionService>();
        var interactionHandler = provider.GetRequiredService<InteractionHandler>();
        await interactionHandler.InitializeAsync();

        var config = provider.GetRequiredService<IConfigurationRoot>();

        var prefixCommands = provider.GetRequiredService<PrefixHandler>();
        prefixCommands.AddModule<PrefixModule>();
        prefixCommands.Initialize();

        client.Log += msg =>
        {
            Console.WriteLine(msg.Message);
            return Task.CompletedTask;
        };
        slashCommands.Log += msg =>
        {
            Console.WriteLine(msg.Message);
            return Task.CompletedTask;
        };
        client.Ready += async () => await slashCommands.RegisterCommandsGloballyAsync();

        await client.LoginAsync(TokenType.Bot, config["token"]);

        await client.StartAsync();

        await Task.Delay(-1);
    }
}