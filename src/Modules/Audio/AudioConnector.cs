using System;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public interface IAudioConnector
{
    Task ConnectAsync(IGuild guild, IVoiceChannel voiceChannel);
}

public sealed class AudioConnector(
    IAudioStore audioStore,
    IAudioDisposer audioDisposer,
    IAudioLogger audioLogger)
    : IAudioConnector
{
    public async Task ConnectAsync(IGuild guild, IVoiceChannel voiceChannel)
    {
        audioLogger.LogWithGuildInfo(guild, $"Checking if bot is already in guild {guild.Name}");
        if (audioStore.GetAudioClientForGuild(guild) is not null)
        {
            audioLogger.LogWithGuildInfo(guild, $"Bot already is in a channel for guild {guild.Name}");
            return;
        }

        var audioClient = await ConnectToVoiceAsync(guild, voiceChannel);
        if (audioClient is null)
            return;

        var didAddSucceed = audioStore.AddAudioClientForGuild(guild, audioClient);
        if (!didAddSucceed)
        {
            await audioDisposer.CleanupAudioClientAsync(guild);
            return;
        }

        audioLogger.LogWithGuildInfo(guild, $"Connected to voice on {guild.Name}");
    }

    private async Task<IAudioClient?> ConnectToVoiceAsync(IGuild guild, IAudioChannel voiceChannel)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Connecting to voice channel");
            return await voiceChannel.ConnectAsync();
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }
    }
}