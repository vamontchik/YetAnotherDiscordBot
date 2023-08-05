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
        await Context.Message.ReplyAsync("pong");
    }

    [Command("rps")]
    public async Task HandleRpsCommandNoArg()
    {
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
            await Context.Message.ReplyAsync("Please specify only one argument");
            return;
        }

        var rpsCommand = new RockPaperScissorsCommand(userEnteredValues.First(), Context, StatsManager);

        await rpsCommand.ExecuteAsync();
    }

    [Command("join")]
    public async Task HandleJoinCommand()
    {
        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        
        if (voiceChannel is null)
        {
            await ReplyAsync("Please join a voice channel first");
            return;
        }

        if (!await AudioService.JoinAudioAsync(Context.Guild, voiceChannel)) 
            await ReplyAsync("Unable to fully connect");
    }

    [Command("leave")]
    public async Task HandleLeaveCommand()
    {
        if (!await AudioService.LeaveAudioAsync(Context.Guild))
            await ReplyAsync("Unable to fully disconnect and clean up resources");
    }

    [Command("play")]
    public async Task HandlePlayCommand([Remainder] string url)
    {
        var userEnteredValues = url
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (userEnteredValues.Count > 1)
        {
            await Context.Message.ReplyAsync("Please specify only one url");
            return;
        }

        if (!await AudioService.SendAudioAsync(Context.Guild, url))
            await ReplyAsync("Something went wrong while playing the song");
    }
}