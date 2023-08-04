using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Interactions;
using DiscordBot.Handler;
using DiscordBot.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RunMode = Discord.Commands.RunMode;

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
                    LogLevel = LogSeverity.Debug
                }))
                .AddSingleton(serviceProvider =>
                    new InteractionService(serviceProvider.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton(_ => new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Debug,
                    DefaultRunMode = RunMode.Async
                }))
                .AddSingleton<PrefixHandler>())
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