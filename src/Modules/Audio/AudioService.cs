using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

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
    private readonly IMusicFileHandler _musicFileHandler;
    private readonly IAudioCleanupOrganizer _audioCleanupOrganizer;
    private readonly IFfmpegHandler _ffmpegHandler;
    private readonly IPcmStreamHandler _pcmStreamHandler;
    private readonly IAudioLogger _audioLogger;

    public AudioService(
        IAudioStore audioStore,
        IAudioConnector audioConnector,
        IMusicFileHandler musicFileHandler,
        IAudioCleanupOrganizer audioCleanupOrganizer,
        IFfmpegHandler ffmpegHandler,
        IPcmStreamHandler pcmStreamHandler,
        IAudioLogger audioLogger)
    {
        _audioStore = audioStore;
        _audioConnector = audioConnector;
        _musicFileHandler = musicFileHandler;
        _audioCleanupOrganizer = audioCleanupOrganizer;
        _ffmpegHandler = ffmpegHandler;
        _pcmStreamHandler = pcmStreamHandler;
        _audioLogger = audioLogger;
    }

    public async Task JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel) =>
        await _audioConnector.ConnectAsync(guild, voiceChannel);

    public async Task LeaveAudioAsync(IGuild guild)
    {
        await _audioCleanupOrganizer.FullDisconnectAndCleanup(guild);
        SetToNoSongPlayingStatus(guild);
    }

    public async Task SendAudioAsync(IGuild guild, string url)
    {
        if (!CheckAndSetCurrentPlayingSong(guild))
            return;

        var audioClient = _audioStore.GetAudioClientForGuild(guild);
        if (audioClient is null)
        {
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var didMusicDownload = await _musicFileHandler.DownloadMusicAsync(guild, url);
        if (!didMusicDownload)
        {
            await _audioCleanupOrganizer.MusicDownloadFailureCleanup(guild);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var result = _ffmpegHandler.SetupFfmpeg(guild, url, out var ffmpegProcess, out var ffmpegStream);
        if (!_ffmpegHandler.CheckAndStore(guild, result, ffmpegProcess, ffmpegStream))
        {
            await _audioCleanupOrganizer.FfmpegSetupFailureCleanup(guild);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var pcmStream = await _pcmStreamHandler.CreatePcmStreamAsync(url, guild, audioClient);
        if (pcmStream is null)
        {
            await _audioCleanupOrganizer.PcmStreamSetupFailureCleanup(guild);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        await SendAudioAsync(guild, url, ffmpegStream!, pcmStream);
        await _pcmStreamHandler.FlushPcmStreamAsync(guild, url, pcmStream);
        await _audioCleanupOrganizer.PostSongCleanup(guild);
        SetToNoSongPlayingStatus(guild);
    }

    private enum SongStatus
    {
        Playing,
        Stopped
    }

    private bool CheckAndSetCurrentPlayingSong(IGuild guild)
    {
        lock (InteractionWithIsPlayingLock)
        {
            if (IsPlayingSong(guild) == SongStatus.Playing)
            {
                _audioLogger.LogWithGuildInfo(guild, "Already playing another song");
                return false;
            }
            
            SetToSongPlayingStatus(guild);
            return true;
        }
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

    public async Task SkipAudioAsync(IGuild guild)
    {
        await _audioCleanupOrganizer.PostSongCleanup(guild);
        SetToNoSongPlayingStatus(guild);
    }

    private bool _isPlaying;
    private static readonly object IsPlayingLock = new();
    private static readonly object InteractionWithIsPlayingLock = new();

    private SongStatus IsPlayingSong(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            _audioLogger.LogWithGuildInfo(guild, $"Retrieving _isPlaying value of {_isPlaying}");
            return _isPlaying ? SongStatus.Playing : SongStatus.Stopped;
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