using System;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public interface IAudioConnector
{
    Task<(bool, string)> ConnectAsync(IGuild guild, IVoiceChannel voiceChannel);
    Task<(bool, string)> DisconnectAsync(IGuild guild);
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

    public async Task<(bool, string)> ConnectAsync(IGuild guild, IVoiceChannel voiceChannel)
    {
        _audioLogger.LogWithGuildInfo(guild, $"Connecting to voice on {guild.Name}");

        if (_audioStore.GetAudioClientForGuild(guild.Id) is not null)
        {
            const string message = "Bot already is in a channel in this guild";
            _audioLogger.LogWithGuildInfo(guild, message);
            return (false, message);
        }

        IAudioClient audioClient;
        try
        {
            audioClient = await voiceChannel.ConnectAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to connect to voice channel");
        }

        var didAddSucceed = _audioStore.AddAudioClientForGuild(guild.Id, audioClient);
        if (!didAddSucceed)
        {
            const string baseErrorMessage = "Unable to add audio client to internal storage";
            var (success, innerErrorMessage) = await _audioDisposer.CleanupAudioClientAsync(guild);
            var fullErrorMessage = success ? baseErrorMessage : innerErrorMessage + " => " + baseErrorMessage;
            return (false, fullErrorMessage);
        }

        _audioLogger.LogWithGuildInfo(guild, $"Connected to voice on {guild.Name}");

        return (true, string.Empty);
    }

    public async Task<(bool, string)> DisconnectAsync(IGuild guild)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        _audioLogger.LogWithGuildInfo(guild,
            $"Disconnecting from voice on {guildName} and disposing of all stream(s)/process(es)/client(s)");

        if (_audioStore.GetAudioClientForGuild(guildId) is null)
        {
            const string message = "Bot is not in a channel in this guild";
            _audioLogger.LogWithGuildInfo(guild, message);
            return (false, message);
        }

        if (_audioStore.GetPcmStreamForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.CleanupPcmStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        if (_audioStore.GetFfmpegStreamForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.CleanupFfmpegStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }


        if (_audioStore.GetFfmpegProcessForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.CleanupFfmpegProcessAsync(guild);
            if (!success)
                return (false, errorMessage);
        }


        if (_audioStore.GetAudioClientForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.CleanupAudioClientAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        _audioLogger.LogWithGuildInfo(guild,
            $"Disconnected from voice on {guildName} and disposed of all stream(s)/process(es)/client(s)");

        return (true, string.Empty);
    }
}