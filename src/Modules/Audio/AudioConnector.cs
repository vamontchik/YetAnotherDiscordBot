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

    public async Task<bool> ConnectAsync(IGuild guildToConnectTo, IVoiceChannel voiceChannelToConnectTo)
    {
        var guildName = guildToConnectTo.Name;
        var guildId = guildToConnectTo.Id;
        var guildIdStr = guildId.ToString();

        if (_audioStore.ContainsAudioClientForGuild(guildId))
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Bot already is in a channel in this guild!");
            return false;
        }

        IAudioClient audioClient;
        try
        {
            audioClient = await voiceChannelToConnectTo.ConnectAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        var didAddSucceed = _audioStore.AddAudioClientForGuild(guildId, audioClient);
        if (!didAddSucceed)
        {
            _ = await _audioDisposer.CleanupAudioClient(guildToConnectTo);
            return false;
        }

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, $"Connected to voice on {guildName}.");
        return true;
    }

    public async Task<bool> DisconnectAsync(IGuild guildToLeaveFrom)
    {
        var guildName = guildToLeaveFrom.Name;
        var guildId = guildToLeaveFrom.Id;
        var guildIdStr = guildId.ToString();

        if (!_audioStore.ContainsAudioClientForGuild(guildId))
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                "Tried to remove the bot from a guild where it is not connected to any voice channel");
            return false;
        }

        try
        {
            await _audioDisposer.CleanupPcmStream(guildToLeaveFrom);
            await _audioDisposer.CleanupFfmpegStreamAsync(guildToLeaveFrom);
            await _audioDisposer.CleanupFfmpegProcessAsync(guildToLeaveFrom);
            await _audioDisposer.CleanupAudioClient(guildToLeaveFrom);

            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                $"Disconnected from voice on {guildName} and disposed of all streams, process, and client");
        }
        catch (Exception e)
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                "Unable to disconnect and dispose of all streams, process, and client");
            Console.WriteLine(e);
            return false;
        }

        return true;
    }
}