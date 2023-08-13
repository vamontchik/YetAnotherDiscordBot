using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public interface IAudioStore
{
    IAudioClient? GetAudioClientForGuild(ulong guildId);
    bool AddAudioClientForGuild(ulong guildId, IAudioClient audioClient);
    bool RemoveAudioClientFromGuild(ulong guildId, out IAudioClient? audioClient);
    Process? GetFfmpegProcessForGuild(ulong guildId);
    bool AddFfmpegProcessForGuild(ulong guildId, Process ffmpegProcess);
    bool RemoveFfmpegProcessFromGuild(ulong guildId, out Process? ffmpegProcess);
    Stream? GetFfmpegStreamForGuild(ulong guildId);
    bool AddFfmpegStreamForGuild(ulong guildId, Stream ffmpegStream);
    bool RemoveFfmpegStreamFromGuild(ulong guildId, out Stream? ffmpegStream);
    AudioOutStream? GetPcmStreamForGuild(ulong guildId);
    bool AddPcmStreamForGuild(ulong guildId, AudioOutStream pcmStream);
    bool RemovePcmStreamFromGuild(ulong guildId, out AudioOutStream? pcmStream);
}

public sealed class AudioStore : IAudioStore
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedAudioClients = new();
    private readonly ConcurrentDictionary<ulong, Process> _connectedFfmpegProcesses = new();
    private readonly ConcurrentDictionary<ulong, Stream> _connectedFfmpegStreams = new();
    private readonly ConcurrentDictionary<ulong, AudioOutStream> _connectedPcmStreams = new();

    #region AudioClient

    public IAudioClient? GetAudioClientForGuild(ulong guildId)
    {
        try
        {
            return _connectedAudioClients[guildId];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public bool AddAudioClientForGuild(ulong guildId, IAudioClient audioClient)
    {
        try
        {
            _connectedAudioClients[guildId] = audioClient;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public bool RemoveAudioClientFromGuild(ulong guildId, out IAudioClient? audioClient)
    {
        try
        {
            return _connectedAudioClients.Remove(guildId, out audioClient);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            audioClient = null;
            return false;
        }
    }

    #endregion

    #region FfmpegProcess

    public Process? GetFfmpegProcessForGuild(ulong guildId)
    {
        try
        {
            return _connectedFfmpegProcesses[guildId];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public bool AddFfmpegProcessForGuild(ulong guildId, Process ffmpegProcess)
    {
        try
        {
            _connectedFfmpegProcesses[guildId] = ffmpegProcess;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public bool RemoveFfmpegProcessFromGuild(ulong guildId, out Process? ffmpegProcess)
    {
        try
        {
            return _connectedFfmpegProcesses.Remove(guildId, out ffmpegProcess);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            ffmpegProcess = null;
            return false;
        }
    }

    #endregion

    #region FfmpegStream

    public Stream? GetFfmpegStreamForGuild(ulong guildId)
    {
        try
        {
            return _connectedFfmpegStreams[guildId];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public bool AddFfmpegStreamForGuild(ulong guildId, Stream ffmpegStream)
    {
        try
        {
            _connectedFfmpegStreams[guildId] = ffmpegStream;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public bool RemoveFfmpegStreamFromGuild(ulong guildId, out Stream? ffmpegStream)
    {
        try
        {
            return _connectedFfmpegStreams.Remove(guildId, out ffmpegStream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            ffmpegStream = null;
            return false;
        }
    }

    #endregion

    #region PcmStream

    public AudioOutStream? GetPcmStreamForGuild(ulong guildId)
    {
        try
        {
            return _connectedPcmStreams[guildId];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public bool AddPcmStreamForGuild(ulong guildId, AudioOutStream pcmStream)
    {
        try
        {
            _connectedPcmStreams[guildId] = pcmStream;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public bool RemovePcmStreamFromGuild(ulong guildId, out AudioOutStream? pcmStream)
    {
        try
        {
            return _connectedPcmStreams.Remove(guildId, out pcmStream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            pcmStream = null;
            return false;
        }
    }

    #endregion
}