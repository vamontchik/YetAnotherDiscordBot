using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

// TODO: this class has too much logic, consider splitting it into several

public interface IAudioService
{
    Task JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel);
    Task LeaveAudioAsync(IGuild guild);
    Task SendAudioAsync(IGuild guild, string url);
    Task SkipAudioAsync(IGuild guild);
}

public sealed class AudioService : IAudioService
{
    private readonly IAudioStore _audioStore;
    private readonly IAudioConnector _audioConnector;
    private readonly IAudioDisposer _audioDisposer;
    private readonly IMusicFileHandler _musicFileHandler;
    private readonly IAudioLogger _audioLogger;

    public AudioService(
        IAudioStore audioStore,
        IAudioConnector audioConnector,
        IAudioDisposer audioDisposer,
        IMusicFileHandler musicFileHandler,
        IAudioLogger audioLogger)
    {
        _audioStore = audioStore;
        _audioConnector = audioConnector;
        _audioDisposer = audioDisposer;
        _musicFileHandler = musicFileHandler;
        _audioLogger = audioLogger;
    }


    public async Task JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel) =>
        await _audioConnector.ConnectAsync(guild, voiceChannel);

    public async Task LeaveAudioAsync(IGuild guild)
    {
        await _audioConnector.DisconnectAsync(guild);
        await _musicFileHandler.DeleteMusicAsync(guild);
        ResetPlayingStatusWithLock(guild);
    }

    public async Task SendAudioAsync(IGuild guild, string url)
    {
        lock (InteractionWithIsPlayingLock)
        {
            if (IsPlayingSong(guild))
            {
                _audioLogger.LogWithGuildInfo(guild, "Already playing another song");
                return;
            }
            SetToSongPlayingStatus(guild);
        }

        var audioClient = _audioStore.GetAudioClientForGuild(guild);
        if (audioClient is null)
        {
            ResetPlayingStatusWithLock(guild);
            return;
        }

        var didMusicDownload = await _musicFileHandler.DownloadMusicAsync(guild, url);
        if (!didMusicDownload)
        {
            await _audioDisposer.CleanupAudioClientAsync(guild);
            await _musicFileHandler.DeleteMusicAsync(guild);
            ResetPlayingStatusWithLock(guild);
            return;
        }

        var successfullySetupFfmpeg = SetupFfmpeg(guild, url, out var ffmpegProcess, out var ffmpegStream);
        if (!successfullySetupFfmpeg ||
            ffmpegProcess is null ||
            ffmpegStream is null ||
            !_audioStore.AddFfmpegProcessForGuild(guild, ffmpegProcess) ||
            !_audioStore.AddFfmpegStreamForGuild(guild, ffmpegStream))
        {
            await _audioDisposer.CleanupFfmpegStreamAsync(guild);
            await _audioDisposer.CleanupFfmpegProcessAsync(guild);
            await _musicFileHandler.DeleteMusicAsync(guild);
            ResetPlayingStatusWithLock(guild);
            return;
        }

        var pcmStream = await CreatePcmStreamAsync(url, guild, audioClient);
        if (pcmStream is null)
        {
            await _audioDisposer.CleanupFfmpegStreamAsync(guild);
            await _audioDisposer.CleanupFfmpegProcessAsync(guild);
            await _audioDisposer.CleanupPcmStreamAsync(guild);
            await _musicFileHandler.DeleteMusicAsync(guild);
            ResetPlayingStatusWithLock(guild);
            return;
        }

        await SendAudioAsync(guild, url, ffmpegStream, pcmStream);

        await FlushPcmStreamAsync(guild, url, pcmStream);
        await _audioDisposer.CleanupPcmStreamAsync(guild);
        await _audioDisposer.CleanupFfmpegStreamAsync(guild);
        await _audioDisposer.CleanupFfmpegProcessAsync(guild);
        await _musicFileHandler.DeleteMusicAsync(guild);
        ResetPlayingStatusWithLock(guild);
    }

    private void ResetPlayingStatusWithLock(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: about to obtain lock");

        lock (InteractionWithIsPlayingLock)
        {
            _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: obtained lock");
            SetToNoSongPlayingStatus(guild);
            _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: reset status");
        }

        _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: released lock");
    }

    private bool SetupFfmpeg(IGuild guild, string url, out Process? ffmpegProcess, out Stream? ffmpegStream)
    {
        ffmpegProcess = null;
        ffmpegStream = null;

        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Creating ffmpeg process");
            var createdProcess = _musicFileHandler.CreateFfmpegProcess();
            if (createdProcess is null)
            {
                _audioLogger.LogWithGuildInfo(guild, "createdProcess is null");
                return false;
            }

            _audioLogger.LogWithGuildInfo(guild, "Retrieving base stream");
            ffmpegProcess = createdProcess;
            ffmpegStream = createdProcess.StandardOutput.BaseStream;
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            ffmpegProcess = null;
            ffmpegStream = null;
            return false;
        }

        _audioLogger.LogWithGuildInfo(guild, $"Created ffmpeg stream of {url} in {guild.Name}");
        return true;
    }

    private async Task<AudioOutStream?> CreatePcmStreamAsync(
        string url,
        IGuild guild,
        IAudioClient client)
    {
        AudioOutStream pcmStream;
        try
        {
            _audioLogger.LogWithGuildInfo(guild, $"Creating pcm stream of {url} in {guild.Name}");
            pcmStream = client.CreatePCMStream(AudioApplication.Mixed);
            _audioLogger.LogWithGuildInfo(guild, $"Created pcm stream of {url} in {guild.Name}");
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            return null;
        }

        return _audioStore.AddPcmStreamForGuild(guild, pcmStream)
            ? pcmStream
            : null;
    }

    private async Task SendAudioAsync(
        IGuild guild,
        string url,
        Stream ffmpegStream,
        Stream pcmStream)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, $"Copying music bytes to pcm stream for {url} in {guild.Name}");
            await ffmpegStream.CopyToAsync(pcmStream);
            _audioLogger.LogWithGuildInfo(guild, $"Copied music bytes to pcm stream for {url} in {guild.Name}");
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    private async Task FlushPcmStreamAsync(
        IGuild guild,
        string url,
        Stream pcmStream)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, $"Flushing pcm stream for {url} in {guild.Name}");
            await pcmStream.FlushAsync();
            _audioLogger.LogWithGuildInfo(guild, $"Flushed pcm stream for {url} in {guild.Name}");
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    public async Task SkipAudioAsync(IGuild guild)
    {
        await _audioDisposer.CleanupPcmStreamAsync(guild);
        await _audioDisposer.CleanupFfmpegStreamAsync(guild);
        await _audioDisposer.CleanupFfmpegProcessAsync(guild);
        await _musicFileHandler.DeleteMusicAsync(guild);
        ResetPlayingStatusWithLock(guild);
    }

    private bool _isPlaying;
    private static readonly object IsPlayingLock = new();
    private static readonly object InteractionWithIsPlayingLock = new();

    private bool IsPlayingSong(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            _audioLogger.LogWithGuildInfo(guild, $"Retrieving _isPlaying value of {_isPlaying}");
            return _isPlaying;
        }
    }

    private void SetToNoSongPlayingStatus(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            _audioLogger.LogWithGuildInfo(guild, $"Setting _isPlaying to false");
            _isPlaying = false;
        }
    }

    private void SetToSongPlayingStatus(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            _audioLogger.LogWithGuildInfo(guild, "Setting _isPlaying to true");
            _isPlaying = true;
        }
    }
}