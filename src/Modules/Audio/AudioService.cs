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

public sealed class AudioService(
    IAudioStore audioStore,
    IAudioConnector audioConnector,
    IMusicFileHandler musicFileHandler,
    IAudioCleanupOrganizer audioCleanupOrganizer,
    IFfmpegHandler ffmpegHandler,
    IPcmStreamHandler pcmStreamHandler,
    IAudioLogger audioLogger)
    : IAudioService
{
    public async Task JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel) =>
        await audioConnector.ConnectAsync(guild, voiceChannel);

    public async Task LeaveAudioAsync(IGuild guild)
    {
        await audioCleanupOrganizer.FullDisconnectAndCleanup(guild);
        SetToNoSongPlayingStatus(guild);
    }

    public async Task SendAudioAsync(IGuild guild, string url)
    {
        if (!CheckAndSetCurrentPlayingSong(guild))
            return;

        var audioClient = audioStore.GetAudioClientForGuild(guild);
        if (audioClient is null)
        {
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var didMusicDownload = await musicFileHandler.DownloadMusicAsync(guild, url);
        if (!didMusicDownload)
        {
            await audioCleanupOrganizer.MusicDownloadFailureCleanup(guild);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var result = ffmpegHandler.SetupFfmpeg(guild, url, out var ffmpegProcess, out var ffmpegStream);
        if (!ffmpegHandler.CheckAndStore(guild, result, ffmpegProcess, ffmpegStream))
        {
            await audioCleanupOrganizer.FfmpegSetupFailureCleanup(guild);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var pcmStream = await pcmStreamHandler.CreatePcmStreamAsync(url, guild, audioClient);
        if (pcmStream is null)
        {
            await audioCleanupOrganizer.PcmStreamSetupFailureCleanup(guild);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        await SendAudioAsync(guild, url, ffmpegStream!, pcmStream);
        await pcmStreamHandler.FlushPcmStreamAsync(guild, url, pcmStream);
        await audioCleanupOrganizer.PostSongCleanup(guild);
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
                audioLogger.LogWithGuildInfo(guild, "Already playing another song");
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
            audioLogger.LogWithGuildInfo(guild, $"Copying music bytes to pcm stream for {url} in {guild.Name}");
            await ffmpegStream.CopyToAsync(pcmStream);
            audioLogger.LogWithGuildInfo(guild, $"Copied music bytes to pcm stream for {url} in {guild.Name}");
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    public async Task SkipAudioAsync(IGuild guild)
    {
        await audioCleanupOrganizer.PostSongCleanup(guild);
        SetToNoSongPlayingStatus(guild);
    }

    private bool _isPlaying;
    private static readonly object IsPlayingLock = new();
    private static readonly object InteractionWithIsPlayingLock = new();

    private SongStatus IsPlayingSong(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            audioLogger.LogWithGuildInfo(guild, $"Retrieving _isPlaying value of {_isPlaying}");
            return _isPlaying ? SongStatus.Playing : SongStatus.Stopped;
        }
    }

    private void SetToNoSongPlayingStatus(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            audioLogger.LogWithGuildInfo(guild, "Setting _isPlaying to false");
            _isPlaying = false;
        }
    }

    private void SetToSongPlayingStatus(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            audioLogger.LogWithGuildInfo(guild, "Setting _isPlaying to true");
            _isPlaying = true;
        }
    }
}