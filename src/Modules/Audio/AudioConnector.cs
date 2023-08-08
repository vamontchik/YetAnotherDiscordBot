using System;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public sealed class AudioConnector
{
    private readonly AudioStore _audioStore;
    private readonly AudioDisposer _audioDisposer;

    public AudioConnector(AudioStore audioStore, AudioDisposer audioDisposer)
    {
        _audioStore = audioStore;
        _audioDisposer = audioDisposer;
    }

    public async Task<(bool, string)> SafeConnectAsync(IGuild guildToConnectTo, IVoiceChannel voiceChannelToConnectTo)
    {
        var guildName = guildToConnectTo.Name;
        var guildId = guildToConnectTo.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, $"Connecting to voice on {guildName}");

        if (_audioStore.GetAudioClientForGuild(guildId) is not null)
        {
            const string message = "Bot already is in a channel in this guild";
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, message);
            return (false, message);
        }

        IAudioClient audioClient;
        try
        {
            audioClient = await voiceChannelToConnectTo.ConnectAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to connect to voice channel");
        }

        var didAddSucceed = _audioStore.AddAudioClientForGuild(guildId, audioClient);
        if (!didAddSucceed)
        {
            const string baseErrorMessage = "Unable to add audio client to internal storage";
            var (success, innerErrorMessage) = await _audioDisposer.SafeCleanupAudioClientAsync(guildToConnectTo);
            var fullErrorMessage = success ? baseErrorMessage : innerErrorMessage + " => " + baseErrorMessage;
            return (false, fullErrorMessage);
        }

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, $"Connected to voice on {guildName}");

        return (true, string.Empty);
    }

    public async Task<(bool, string)> SafeDisconnectAsync(IGuild guild)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Disconnecting from voice on {guildName} and disposing of all stream(s)/process(es)/client(s)");

        if (_audioStore.GetAudioClientForGuild(guildId) is null)
        {
            const string message = "Bot is not in a channel in this guild";
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, message);
            return (false, message);
        }

        if (_audioStore.GetPcmStreamForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupPcmStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        if (_audioStore.GetFfmpegStreamForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupFfmpegStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }


        if (_audioStore.GetFfmpegProcessForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupFfmpegProcessAsync(guild);
            if (!success)
                return (false, errorMessage);
        }


        if (_audioStore.GetAudioClientForGuild(guildId) is not null)
        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupAudioClientAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Disconnected from voice on {guildName} and disposed of all stream(s)/process(es)/client(s)");

        return (true, string.Empty);
    }
}