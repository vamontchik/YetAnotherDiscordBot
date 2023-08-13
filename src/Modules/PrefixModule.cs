using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Modules.Audio;
using DiscordBot.Modules.RockPaperScissors;

namespace DiscordBot.Modules;

public sealed class PrefixModule : ModuleBase<SocketCommandContext>
{
    public StatsManager StatsManager { get; set; }
    public AudioService AudioService { get; set; }

    [Command("ping")]
    public async Task HandlePingCommand()
    {
        LogMessageWithContext("Ping command");
        await Context.Message.ReplyAsync("pong");
    }

    [Command("rps")]
    public async Task HandleRpsCommandNoArg()
    {
        LogMessageWithContext("Rock-paper-scissors command with no argument");
        await Context.Message.ReplyAsync("Please specify an argument");
    }

    [Command("rps")]
    public async Task HandleRpsCommand([Remainder] string choice)
    {
        var userEnteredValues = choice
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (userEnteredValues.Count > 1)
        {
            LogMessageWithContext("Rock-paper-scissors command with too many arguments");
            await Context.Message.ReplyAsync("Please specify only one argument");
            return;
        }

        LogMessageWithContext("Rock-paper-scissors command");
        var argument = userEnteredValues.First();
        var isStatCheck = argument.Equals("stat") || argument.Equals("stats");
        var rpsCommand = new RockPaperScissorsCommand
        {
            Argument = argument,
            IsStatCheck = isStatCheck,
            SocketCommandContext = Context,
            StatsManager = StatsManager
        };
        await rpsCommand.ExecuteAsync();
    }

    [Command("join")]
    public async Task HandleJoinCommand()
    {
        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;

        if (voiceChannel is null)
        {
            LogMessageWithContext("Join command but the user was not in a voice channel");
            await Context.Message.ReplyAsync("Please join a voice channel first");
            return;
        }

        LogMessageWithContext("Join command");
        var (success, errorMessage) = await AudioService.JoinAudioAsync(Context.Guild, voiceChannel);
        if (!success)
        {
            LogErrorMessageWithContext(errorMessage, "join");
            await Context.Message.ReplyAsync(errorMessage);
        }
    }

    [Command("leave")]
    public async Task HandleLeaveCommand()
    {
        LogMessageWithContext("Leave command");
        var (success, errorMessage) = await AudioService.LeaveAudioAsync(Context.Guild);
        if (!success)
        {
            LogErrorMessageWithContext(errorMessage, "leave");
            await Context.Message.ReplyAsync(errorMessage);
        }
    }

    [Command("play")]
    public async Task HandlePlayCommandNoArg()
    {
        LogMessageWithContext("Play command with no arguments");
        await Context.Message.ReplyAsync("Please specify a url");
    }

    [Command("play")]
    public async Task HandlePlayCommand([Remainder] string url)
    {
        var userEnteredValues = url
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (userEnteredValues.Count > 1)
        {
            LogMessageWithContext("Play command with too many arguments");
            await Context.Message.ReplyAsync("Please specify only one url");
            return;
        }

        LogMessageWithContext("Play command");
        var (success, errorMessage) = await AudioService.SendAudioAsync(Context.Guild, url);
        if (!success)
        {
            LogErrorMessageWithContext(errorMessage, "play");
            await Context.Message.ReplyAsync(errorMessage);
        }
    }

    [Command("skip")]
    public async Task HandleSkipCommand()
    {
        LogMessageWithContext("Skip command");
        var (success, errorMessage) = await AudioService.SkipAudioAsync(Context.Guild);
        if (!success)
        {
            LogErrorMessageWithContext(errorMessage, "skip");
            await Context.Message.ReplyAsync(errorMessage);
        }
    }

    private void LogMessageWithContext(string message) =>
        Console.WriteLine(message
                          + $": requested by {Context.User.Username},{Context.User.Id} in {Context.Guild}");

    private void LogErrorMessageWithContext(string message, string typeOfCommand) =>
        Console.WriteLine(message
                          + $": requested by {Context.User.Username},{Context.User.Id} in {Context.Guild}"
                          + $"Originated from a {typeOfCommand} command");
}