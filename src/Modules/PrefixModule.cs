using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Modules.Audio;
using DiscordBot.Modules.RockPaperScissors;

namespace DiscordBot.Modules;

internal sealed class PrefixModule(IStatsManager statsManager, IAudioService audioService)
    : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    public async Task HandlePingCommand()
    {
        LogMessageWithContext("Ping command");
        await Context.Message.ReplyAsync("pong").ConfigureAwait(false);
    }

    [Command("rps")]
    public async Task HandleRpsCommandNoArg()
    {
        LogMessageWithContext("Rock-paper-scissors command with no argument");
        await Context.Message.ReplyAsync("Please specify an argument").ConfigureAwait(false);
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
            await Context.Message.ReplyAsync("Please specify only one argument").ConfigureAwait(false);
            return;
        }

        LogMessageWithContext("Rock-paper-scissors command");
        var argument = userEnteredValues.First();
        var rpsCommand = new RockPaperScissorsCommand
        {
            Argument = argument,
            SocketCommandContext = Context,
            StatsManager = statsManager
        };
        await rpsCommand.ExecuteAsync().ConfigureAwait(false);
    }

    [Command("join")]
    public async Task HandleJoinCommand()
    {
        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;

        if (voiceChannel is null)
        {
            LogMessageWithContext("Join command but the user was not in a voice channel");
            await Context.Message.ReplyAsync("Please join a voice channel first").ConfigureAwait(false);
            return;
        }

        LogMessageWithContext("Join command");
        await audioService.JoinAudioAsync(Context.Guild, voiceChannel).ConfigureAwait(false);
    }

    [Command("leave")]
    public async Task HandleLeaveCommand()
    {
        LogMessageWithContext("Leave command");
        await audioService.LeaveAudioAsync(Context.Guild).ConfigureAwait(false);
    }

    [Command("play")]
    public async Task HandlePlayCommandNoArg()
    {
        LogMessageWithContext("Play command with no arguments");
        await Context.Message.ReplyAsync("Please specify a url").ConfigureAwait(false);
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
            await Context.Message.ReplyAsync("Please specify only one url").ConfigureAwait(false);
            return;
        }

        LogMessageWithContext("Play command");
        await audioService.SendAudioAsync(Context.Guild, url).ConfigureAwait(false);
    }

    [Command("skip")]
    public async Task HandleSkipCommand()
    {
        LogMessageWithContext("Skip command");
        await audioService.SkipAudioAsync(Context.Guild).ConfigureAwait(false);
    }

    [Command("help")]
    public async Task HandleHelpCommand()
    {
        LogMessageWithContext("Help command");
        var helpCommand = new HelpCommand(Context);
        await helpCommand.ExecuteAsync().ConfigureAwait(false);
    }

    private void LogMessageWithContext(string message) =>
        Console.WriteLine(message
                          + $": requested by {Context.User.Username},{Context.User.Id} in {Context.Guild}");
}