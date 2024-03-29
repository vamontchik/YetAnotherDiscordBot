﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Modules.Audio;
using DiscordBot.Modules.RockPaperScissors;

namespace DiscordBot.Modules;

public sealed class PrefixModule : ModuleBase<SocketCommandContext>
{
    private readonly IStatsManager _statsManager;
    private readonly IAudioService _audioService;

    public PrefixModule(
        IStatsManager statsManager,
        IAudioService audioService)
    {
        _statsManager = statsManager;
        _audioService = audioService;
    }


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
        var rpsCommand = new RockPaperScissorsCommand
        {
            Argument = argument,
            SocketCommandContext = Context,
            StatsManager = _statsManager
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
        await _audioService.JoinAudioAsync(Context.Guild, voiceChannel);
    }

    [Command("leave")]
    public async Task HandleLeaveCommand()
    {
        LogMessageWithContext("Leave command");
        await _audioService.LeaveAudioAsync(Context.Guild);
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
        await _audioService.SendAudioAsync(Context.Guild, url);
    }

    [Command("skip")]
    public async Task HandleSkipCommand()
    {
        LogMessageWithContext("Skip command");
        await _audioService.SkipAudioAsync(Context.Guild);
    }

    private void LogMessageWithContext(string message) =>
        Console.WriteLine(message
                          + $": requested by {Context.User.Username},{Context.User.Id} in {Context.Guild}");
}