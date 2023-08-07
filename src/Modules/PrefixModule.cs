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

    private const string Separator = "--------------------";

    [Command("ping")]
    public async Task HandlePingCommand()
    {
        Console.WriteLine($"{Separator}PING{Separator}");

        await Context.Message.ReplyAsync("pong");
    }

    [Command("rps")]
    public async Task HandleRpsCommandNoArg()
    {
        Console.WriteLine($"{Separator}ROCK_PAPER_SCISSORS_NO_ARG{Separator}");
        await LogAndReply("Please specify an argument");
    }

    [Command("rps")]
    public async Task HandleRpsCommand([Remainder] string choice)
    {
        Console.WriteLine($"{Separator}ROCK_PAPER_SCISSORS{Separator}");

        var userEnteredValues = choice
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (userEnteredValues.Count > 1)
        {
            await LogAndReply("Please specify only one argument");
            return;
        }

        var rpsCommand = new RockPaperScissorsCommand(userEnteredValues.First(), Context, StatsManager);

        await rpsCommand.ExecuteAsync();
    }

    [Command("join")]
    public async Task HandleJoinCommand()
    {
        Console.WriteLine($"{Separator}JOIN{Separator}");

        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;

        if (voiceChannel is null)
        {
            await LogAndReply("Please join a voice channel first");
            return;
        }

        if (!await AudioService.JoinAudioAsync(Context.Guild, voiceChannel))
            await LogAndReply("Unable to fully connect");
    }

    [Command("leave")]
    public async Task HandleLeaveCommand()
    {
        Console.WriteLine($"{Separator}LEAVE{Separator}");

        if (!await AudioService.LeaveAudioAsync(Context.Guild))
            await LogAndReply("Unable to fully disconnect and clean up resources");
    }

    [Command("play")]
    public async Task HandlePlayCommandNoArg()
    {
        Console.WriteLine($"{Separator}PLAY_NO_ARG{Separator}");

        await LogAndReply("Please specify a url");
    }

    [Command("play")]
    public async Task HandlePlayCommand([Remainder] string url)
    {
        Console.WriteLine($"{Separator}PLAY{Separator}");

        var userEnteredValues = url
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (userEnteredValues.Count > 1)
        {
            await LogAndReply("Please specify only one url");
            return;
        }

        if (!await AudioService.SendAudioAsync(Context.Guild, url))
            await LogAndReply("Something went wrong while playing the song");
    }

    [Command("skip")]
    public async Task HandleSkipCommand()
    {
        Console.WriteLine($"{Separator}SKIP{Separator}");

        if (!await AudioService.SkipAudioAsync(Context.Guild))
            await LogAndReply("Something went wrong when skipping the current song");
    }

    private async Task LogAndReply(string message)
    {
        Console.WriteLine(message);
        await Context.Message.ReplyAsync(message);
    }
}