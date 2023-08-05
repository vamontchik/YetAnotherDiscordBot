using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public sealed class AudioService
{
    private readonly AudioStore _audioStore;
    private readonly AudioConnector _audioConnector;
    private readonly AudioDisposer _audioDisposer;

    public AudioService(
        AudioStore audioStore,
        AudioConnector audioConnector,
        AudioDisposer audioDisposer)
    {
        _audioStore = audioStore;
        _audioConnector = audioConnector;
        _audioDisposer = audioDisposer;
    }


    public async Task<bool> JoinAudioAsync(IGuild guildToConnectTo, IVoiceChannel voiceChannelToConnectTo) =>
        await _audioConnector.Connect(guildToConnectTo, voiceChannelToConnectTo);

    public async Task<bool> LeaveAudioAsync(IGuild guildToLeaveFrom)
    {
        var didDisconnectSucceed = await _audioConnector.Disconnect(guildToLeaveFrom);
        var didDeletionSucceed = await MusicFileHandler.SafeDeleteMusic(guildToLeaveFrom);
        lock (InteractionWithIsPlayingLock)
            SetToNoSongPlayingStatus();
        return didDisconnectSucceed && didDeletionSucceed;
    }

    public async Task<bool> SendAudioAsync(IGuild guild, string url)
    {
        lock (InteractionWithIsPlayingLock)
        {
            if (IsPlayingSong())
                return false;

            SetToSongPlayingStatus();
        }

        if (!_audioStore.ContainsAudioClientForGuild(guild.Id))
        {
            ResetPlayingStatusWithLock();
            return false;
        }

        var audioClient = _audioStore.GetAudioClientForGuild(guild.Id)!;

        if (!await MusicFileHandler.SafeDownloadMusic(guild, url))
        {
            ResetPlayingStatusWithLock();
            return false;
        }

        if (!await SetupFfmpegWithExceptionHandling(guild, url, out var ffmpegProcess, out var ffmpegStream))
        {
            _ = await MusicFileHandler.SafeDeleteMusic(guild); // TODO: return value?
            ResetPlayingStatusWithLock();
            return false;
        }

        if (ffmpegProcess is null || ffmpegStream is null)
        {
            _ = await MusicFileHandler.SafeDeleteMusic(guild); // TODO: return value ?
            ResetPlayingStatusWithLock();
            return false;
        }

        var pcmStream = await CreatePcmStreamWithExceptionHandling(url, guild, audioClient);
        if (pcmStream is null)
        {
            _ = await _audioDisposer.CleanupFfmpegProcess(guild);
            _ = await _audioDisposer.CleanupFfmpegStream(guild);
            _ = await MusicFileHandler.SafeDeleteMusic(guild); // TODO: return value ?
            ResetPlayingStatusWithLock();
            return false;
        }

        if (!await SendAudioWithExceptionHandling(guild, url, ffmpegStream, pcmStream))
        {
            ResetPlayingStatusWithLock();
            return false;
        }

        lock (InteractionWithIsPlayingLock)
        {
            ResetPlayingStatusWithLock();
        }

        return true;

        void ResetPlayingStatusWithLock()
        {
            lock (InteractionWithIsPlayingLock)
                SetToNoSongPlayingStatus();
        }
    }

    private async Task<bool> SendAudioWithExceptionHandling(
        IGuild guild,
        string url,
        Stream ffmpegStream,
        AudioOutStream pcmStream)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        bool didFlushSucceed;
        bool didCleanupSucceed;
        bool didFileDeletionSucceed;
        try
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                $"Copying music bytes to pcm stream for {url} in {guildName}");
            await ffmpegStream.CopyToAsync(pcmStream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                $"Flushing pcm stream for {url} in {guildName}");

            didFlushSucceed = await FlushPcmStreamWithExceptionHandling(guild, pcmStream);
            didCleanupSucceed = await CleanupAfterSongEndsWithExceptionHandling(guild);
            didFileDeletionSucceed = await MusicFileHandler.SafeDeleteMusic(guild);
        }

        return didFlushSucceed &&
               didCleanupSucceed &&
               didFileDeletionSucceed;
    }

    private static async Task<bool> FlushPcmStreamWithExceptionHandling(IGuild guild, AudioOutStream pcmStream)
    {
        var guildName = guild.Name;
        var guildIdStr = guild.Id.ToString();

        try
        {
            await pcmStream.FlushAsync();
        }
        catch (Exception e)
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Failed to flush final bytes");
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    private async Task<AudioOutStream?> CreatePcmStreamWithExceptionHandling(
        string url,
        IGuild guild,
        IAudioClient client)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, $"Creating pcm stream of {url} in {guildName}");

        AudioOutStream pcmStream;
        try
        {
            pcmStream = client.CreatePCMStream(AudioApplication.Mixed);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _ = await MusicFileHandler.SafeDeleteMusic(guild); // TODO: return value?
            return null;
        }

        _audioStore.AddPcmStreamForGuild(guildId, pcmStream);

        return pcmStream;
    }

    private Task<bool> SetupFfmpegWithExceptionHandling(
        IGuild guild,
        string url,
        out Process? ffmpegProcess,
        out Stream? ffmpegStream)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Creating ffmpeg stream of {url} in {guildName}");

        ffmpegProcess = null;
        ffmpegStream = null;

        try
        {
            var createdProcess = CreateFfmpegProcess();

            if (createdProcess is null)
            {
                AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "createdProcess was null");
                return Task.FromResult(false);
            }

            var baseStream = createdProcess.StandardOutput.BaseStream;

            ffmpegProcess = createdProcess;
            ffmpegStream = baseStream;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            ffmpegProcess = null;
            ffmpegStream = null;

            return Task.FromResult(false);
        }

        _audioStore.AddFfmpegProcessForGuild(guildId, ffmpegProcess);
        _audioStore.AddFfmpegStreamForGuild(guildId, ffmpegStream);

        return Task.FromResult(true);
    }

    private static Process? CreateFfmpegProcess()
    {
        var fullPath = MusicFileHandler.GetFullPathToDownloadedFile();
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{fullPath}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        return Process.Start(startInfo);
    }


    private async Task<bool> CleanupAfterSongEndsWithExceptionHandling(IGuild guild)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        bool didFfmpegProcessCleanupSucceed;
        bool didFfmpegStreamCleanupSucceed;
        bool didPcmStreamCleanupSucceed;
        try
        {
            didFfmpegProcessCleanupSucceed = await _audioDisposer.CleanupFfmpegProcess(guild);
            didFfmpegStreamCleanupSucceed = await _audioDisposer.CleanupFfmpegStream(guild);
            didPcmStreamCleanupSucceed = await _audioDisposer.CleanupPcmStream(guild);
        }
        catch (Exception e)
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Unable to fully cleanup");
            Console.WriteLine(e);

            return false;
        }

        return didFfmpegProcessCleanupSucceed &&
               didFfmpegStreamCleanupSucceed &&
               didPcmStreamCleanupSucceed;
    }

    private bool _isPlaying;
    private static readonly object IsPlayingLock = new();
    private static readonly object InteractionWithIsPlayingLock = new();

    private bool IsPlayingSong()
    {
        lock (IsPlayingLock)
        {
            return _isPlaying;
        }
    }

    private void SetToNoSongPlayingStatus()
    {
        lock (IsPlayingLock)
        {
            _isPlaying = false;
        }
    }

    private void SetToSongPlayingStatus()
    {
        lock (IsPlayingLock)
        {
            _isPlaying = true;
        }
    }
}