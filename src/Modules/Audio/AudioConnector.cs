using System;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public interface IAudioConnector
{
    Task ConnectAsync(IGuild guild, IVoiceChannel voiceChannel);
    Task DisconnectAsync(IGuild guild);
}

public sealed class AudioConnector : IAudioConnector
{
    private readonly IAudioStore _audioStore;
    private readonly IAudioDisposer _audioDisposer;
    private readonly IAudioLogger _audioLogger;

    public AudioConnector(
        IAudioStore audioStore,
        IAudioDisposer audioDisposer,
        IAudioLogger audioLogger)
    {
        _audioStore = audioStore;
        _audioDisposer = audioDisposer;
        _audioLogger = audioLogger;
    }

    public async Task ConnectAsync(IGuild guild, IVoiceChannel voiceChannel)
    {
        _audioLogger.LogWithGuildInfo(guild, $"Checking if bot is already in guild {guild.Name}");
        if (_audioStore.GetAudioClientForGuild(guild) is not null)
        {
            _audioLogger.LogWithGuildInfo(guild, $"Bot already is in a channel for guild {guild.Name}");
            return;
        }

        var audioClient = await ConnectToVoiceAsync(guild, voiceChannel);
        if (audioClient is null)
            return;

        var didAddSucceed = _audioStore.AddAudioClientForGuild(guild, audioClient);
        if (!didAddSucceed)
        {
            await _audioDisposer.CleanupAudioClientAsync(guild);
            return;
        }

        _audioLogger.LogWithGuildInfo(guild, $"Connected to voice on {guild.Name}");
    }

    private async Task<IAudioClient?> ConnectToVoiceAsync(IGuild guild, IAudioChannel voiceChannel)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Connecting to voice channel");
            return await voiceChannel.ConnectAsync();
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }
    }

    public async Task DisconnectAsync(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, $"Checking if bot is connected to a channel in guild {guild.Name}");
        if (_audioStore.GetAudioClientForGuild(guild) is null)
        {
            _audioLogger.LogWithGuildInfo(guild, $"Bot is not in a channel for guild {guild.Name}");
            return;
        }

        if (_audioStore.GetPcmStreamForGuild(guild) is not null)
            await _audioDisposer.CleanupPcmStreamAsync(guild);

        if (_audioStore.GetFfmpegStreamForGuild(guild) is not null)
            await _audioDisposer.CleanupFfmpegStreamAsync(guild);

        if (_audioStore.GetFfmpegProcessForGuild(guild) is not null)
            await _audioDisposer.CleanupFfmpegProcessAsync(guild);

        if (_audioStore.GetAudioClientForGuild(guild) is not null)
            await _audioDisposer.CleanupAudioClientAsync(guild);

        _audioLogger.LogWithGuildInfo(
            guild,
            $"Disconnected from voice on {guild.Name} and disposed of all stream(s)/process(es)/client(s)");
    }
}