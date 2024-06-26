﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

internal interface IAudioStore
{
    IAudioClient? GetAudioClientForGuild(IGuild guild);
    bool AddAudioClientForGuild(IGuild guild, IAudioClient audioClient);
    bool RemoveAudioClientFromGuild(IGuild guild, out IAudioClient? audioClient);
    Process? GetFfmpegProcessForGuild(IGuild guild);
    bool AddFfmpegProcessForGuild(IGuild guild, Process ffmpegProcess);
    bool RemoveFfmpegProcessFromGuild(IGuild guild, out Process? ffmpegProcess);
    Stream? GetFfmpegStreamForGuild(IGuild guild);
    bool AddFfmpegStreamForGuild(IGuild guild, Stream ffmpegStream);
    bool RemoveFfmpegStreamFromGuild(IGuild guild, out Stream? ffmpegStream);
    AudioOutStream? GetPcmStreamForGuild(IGuild guild);
    bool AddPcmStreamForGuild(IGuild guild, AudioOutStream pcmStream);
    bool RemovePcmStreamFromGuild(IGuild guild, out AudioOutStream? pcmStream);
}

internal sealed class AudioStore(IAudioLogger audioLogger) : IAudioStore
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedAudioClients = new();
    private readonly ConcurrentDictionary<ulong, Process> _connectedFfmpegProcesses = new();
    private readonly ConcurrentDictionary<ulong, Stream> _connectedFfmpegStreams = new();
    private readonly ConcurrentDictionary<ulong, AudioOutStream> _connectedPcmStreams = new();

    #region AudioClient

    public IAudioClient? GetAudioClientForGuild(IGuild guild)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Retrieving stored audio client");
            return _connectedAudioClients[guild.Id];
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }
    }

    public bool AddAudioClientForGuild(IGuild guild, IAudioClient audioClient)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Storing audio client");
            _connectedAudioClients[guild.Id] = audioClient;
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    public bool RemoveAudioClientFromGuild(IGuild guild, out IAudioClient? audioClient)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Removing stored audio client");
            return _connectedAudioClients.Remove(guild.Id, out audioClient);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            audioClient = null;
            return false;
        }
    }

    #endregion

    #region FfmpegProcess

    public Process? GetFfmpegProcessForGuild(IGuild guild)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Retrieving stored ffmpeg process");
            return _connectedFfmpegProcesses[guild.Id];
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }
    }

    public bool AddFfmpegProcessForGuild(IGuild guild, Process ffmpegProcess)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Storing ffmpeg process");
            _connectedFfmpegProcesses[guild.Id] = ffmpegProcess;
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    public bool RemoveFfmpegProcessFromGuild(IGuild guild, out Process? ffmpegProcess)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Removing stored ffmpeg process");
            return _connectedFfmpegProcesses.Remove(guild.Id, out ffmpegProcess);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            ffmpegProcess = null;
            return false;
        }
    }

    #endregion

    #region FfmpegStream

    public Stream? GetFfmpegStreamForGuild(IGuild guild)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Retrieving stored ffmpeg stream");
            return _connectedFfmpegStreams[guild.Id];
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }
    }

    public bool AddFfmpegStreamForGuild(IGuild guild, Stream ffmpegStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Storing ffmpeg stream");
            _connectedFfmpegStreams[guild.Id] = ffmpegStream;
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    public bool RemoveFfmpegStreamFromGuild(IGuild guild, out Stream? ffmpegStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Removing stored ffmpeg stream");
            return _connectedFfmpegStreams.Remove(guild.Id, out ffmpegStream);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            ffmpegStream = null;
            return false;
        }
    }

    #endregion

    #region PcmStream

    public AudioOutStream? GetPcmStreamForGuild(IGuild guild)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Retrieving stored pcm stream");
            return _connectedPcmStreams[guild.Id];
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }
    }

    public bool AddPcmStreamForGuild(IGuild guild, AudioOutStream pcmStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Storing pcm stream");
            _connectedPcmStreams[guild.Id] = pcmStream;
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    public bool RemovePcmStreamFromGuild(IGuild guild, out AudioOutStream? pcmStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Removing stored pcm stream");
            return _connectedPcmStreams.Remove(guild.Id, out pcmStream);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            pcmStream = null;
            return false;
        }
    }

    #endregion
}