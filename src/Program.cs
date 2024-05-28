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
                .AddSingleton<IStatsManager, StatsManager>()
                .AddSingleton<IAudioService, AudioService>()
                .AddSingleton<IAudioStore, AudioStore>()
                .AddSingleton<IAudioConnector, AudioConnector>()
                .AddSingleton<IAudioDisposer, AudioDisposer>()
                .AddSingleton<IMusicFileHandler, MusicFileHandler>()
                .AddSingleton<IAudioLogger, AudioLogger>()
                .AddSingleton<IAudioCleanupOrganizer, AudioCleanupOrganizer>()
                .AddSingleton<IFfmpegHandler, FfmpegHandler>()
                .AddSingleton<IPcmStreamHandler, PcmStreamHandler>()
            )
            .Build();

        await RunAsync(host);
    }

    private static async Task RunAsync(IHost host)
    {
        using var serviceScope = host.Services.CreateScope();
        var provider = serviceScope.ServiceProvider;

        var interactionService = await InitializeInteractions(provider);
        InitializePrefixCommands(provider);
        var client = provider.GetRequiredService<DiscordSocketClient>();
        var config = provider.GetRequiredService<IConfigurationRoot>();
        var commandService = provider.GetRequiredService<CommandService>();

        SetupClientCallbacks(client, interactionService);
        SetupInteractionServiceCallbacks(interactionService);
        SetupCommandServiceCallbacks(commandService);

        await client.LoginAsync(TokenType.Bot, config["token"]);

        await client.StartAsync();

        await Task.Delay(-1);
    }

    private static void SetupCommandServiceCallbacks(CommandService commandService)
    {
        commandService.Log += message =>
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases[0]}"
                                  + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");

            return Task.CompletedTask;
        };
    }

    private static void SetupInteractionServiceCallbacks(InteractionService interactionService)
    {
        interactionService.Log += message =>
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases[0]}"
                                  + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");

            return Task.CompletedTask;
        };
    }

    private static void SetupClientCallbacks(DiscordSocketClient client, InteractionService interactionService)
    {
        client.Log += message =>
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases[0]}"
                                  + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");

            return Task.CompletedTask;
        };
        client.Ready += async () => await interactionService.RegisterCommandsGloballyAsync();
    }

    private static async Task<InteractionService> InitializeInteractions(IServiceProvider provider)
    {
        var interactionService = provider.GetRequiredService<InteractionService>();
        var interactionHandler = provider.GetRequiredService<InteractionHandler>();
        await interactionHandler.InitializeAsync();
        return interactionService;
    }

    private static void InitializePrefixCommands(IServiceProvider provider)
    {
        var prefixCommands = provider.GetRequiredService<PrefixHandler>();
        prefixCommands.AddModule<PrefixModule>();
        prefixCommands.Initialize();
    }
}