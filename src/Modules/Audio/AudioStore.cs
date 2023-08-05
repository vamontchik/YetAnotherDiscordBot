using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public sealed class AudioStore
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedAudioClients = new();
    private readonly ConcurrentDictionary<ulong, Process> _connectedFfmpegProcesses = new();
    private readonly ConcurrentDictionary<ulong, Stream> _connectedFfmpegStreams = new();
    private readonly ConcurrentDictionary<ulong, AudioOutStream> _connectedPcmStreams = new();

    #region AudioClient

    public bool ContainsAudioClientForGuild(ulong guildId) =>
        _connectedAudioClients.ContainsKey(guildId);

    public IAudioClient? GetAudioClientForGuild(ulong guildId)
    {
        _ = _connectedAudioClients.TryGetValue(guildId, out var audioClient);
        return audioClient;
    }

    public bool AddAudioClientForGuild(ulong guildId, IAudioClient audioClient) =>
        _connectedAudioClients.TryAdd(guildId, audioClient);

    public bool RemoveAudioClientFromGuild(ulong guildId, out IAudioClient? audioClient) =>
        _connectedAudioClients.TryRemove(guildId, out audioClient);

    #endregion

    #region FfmpegProcess

    public bool ContainsFfmpegProcessForGuild(ulong guildId) =>
        _connectedFfmpegProcesses.ContainsKey(guildId);

    public Process? GetFfmpegProcessForGuild(ulong guildId)
    {
        _ = _connectedFfmpegProcesses.TryGetValue(guildId, out var ffmpegProcess);
        return ffmpegProcess;
    }

    public bool AddFfmpegProcessForGuild(ulong guildId, Process ffmpegProcess) =>
        _connectedFfmpegProcesses.TryAdd(guildId, ffmpegProcess);

    public bool RemoveFfmpegProcessFromGuild(ulong guildId, out Process? ffmpegProcess) =>
        _connectedFfmpegProcesses.TryRemove(guildId, out ffmpegProcess);

    #endregion

    #region FfmpegStream

    public bool ContainsFfmpegStreamForGuild(ulong guildId) =>
        _connectedFfmpegStreams.ContainsKey(guildId);

    public Stream? GetFfmpegStreamForGuild(ulong guildId)
    {
        _ = _connectedFfmpegStreams.TryGetValue(guildId, out var ffmpegStream);
        return ffmpegStream;
    }

    public bool AddFfmpegStreamForGuild(ulong guildId, Stream ffmpegStream) =>
        _connectedFfmpegStreams.TryAdd(guildId, ffmpegStream);

    public bool RemoveFfmpegStreamFromGuild(ulong guildId, out Stream? ffmpegStream) =>
        _connectedFfmpegStreams.TryRemove(guildId, out ffmpegStream);

    #endregion

    #region PcmStream

    public bool ContainsPcmStreamForGuild(ulong guildId) =>
        _connectedPcmStreams.ContainsKey(guildId);

    public AudioOutStream? GetPcmStreamForGuild(ulong guildId)
    {
        _ = _connectedPcmStreams.TryGetValue(guildId, out var pcmStream);
        return pcmStream;
    }

    public bool AddPcmStreamForGuild(ulong guildId, AudioOutStream pcmStream) =>
        _connectedPcmStreams.TryAdd(guildId, pcmStream);

    public bool RemovePcmStreamFromGuild(ulong guildId, out AudioOutStream? pcmStream) =>
        _connectedPcmStreams.TryRemove(guildId, out pcmStream);

    #endregion
}