﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

internal interface IAudioConnector
{
    Task ConnectAsync(IGuild guild, IVoiceChannel voiceChannel);
}

internal sealed class AudioConnector(
    IAudioStore audioStore,
    IAudioDisposer audioDisposer,
    IAudioLogger audioLogger)
    : IAudioConnector
{
    public async Task ConnectAsync(IGuild guild, IVoiceChannel voiceChannel)
    {
        audioLogger.LogWithGuildInfo(guild, "Checking if bot is in a channel");
        if (audioStore.GetAudioClientForGuild(guild) is not null)
        {
            audioLogger.LogWithGuildInfo(guild, "Bot already is in a channel");
            return;
        }

        var audioClient = await ConnectToVoiceAsync(guild, voiceChannel).ConfigureAwait(false);
        if (audioClient is null)
            return;

        var didAddSucceed = audioStore.AddAudioClientForGuild(guild, audioClient);
        if (!didAddSucceed)
        {
            await audioDisposer.CleanupAudioClientAsync(guild).ConfigureAwait(false);
            return;
        }

        audioLogger.LogWithGuildInfo(guild, "Connected to voice");
    }

    private async Task<IAudioClient?> ConnectToVoiceAsync(IGuild guild, IAudioChannel voiceChannel)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Connecting to voice channel");
            return await voiceChannel.ConnectAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }
    }
}