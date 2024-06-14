using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

internal interface IAudioService
{
    Task JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel);
    Task LeaveAudioAsync(IGuild guild);
    Task SendAudioAsync(IGuild guild, string url);
    Task SkipAudioAsync(IGuild guild);
}

internal sealed class AudioService(
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
        await audioConnector.ConnectAsync(guild, voiceChannel).ConfigureAwait(false);

    public async Task LeaveAudioAsync(IGuild guild)
    {
        await audioCleanupOrganizer.FullDisconnectAndCleanup(guild).ConfigureAwait(false);
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

        var didMusicDownload = await musicFileHandler.DownloadMusicAsync(guild, url).ConfigureAwait(false);
        if (!didMusicDownload)
        {
            await audioCleanupOrganizer.MusicDownloadFailureCleanup(guild).ConfigureAwait(false);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var result = ffmpegHandler.SetupFfmpeg(guild, out var ffmpegProcess, out var ffmpegStream);
        if (!ffmpegHandler.CheckAndStore(guild, result, ffmpegProcess, ffmpegStream))
        {
            await audioCleanupOrganizer.FfmpegSetupFailureCleanup(guild).ConfigureAwait(false);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        var pcmStream = await pcmStreamHandler.CreatePcmStreamAsync(url, guild, audioClient).ConfigureAwait(false);
        if (pcmStream is null)
        {
            await audioCleanupOrganizer.PcmStreamSetupFailureCleanup(guild).ConfigureAwait(false);
            SetToNoSongPlayingStatus(guild);
            return;
        }

        await SendAudioAsync(guild, ffmpegStream!, pcmStream).ConfigureAwait(false);
        await pcmStreamHandler.FlushPcmStreamAsync(guild, url, pcmStream).ConfigureAwait(false);
        await audioCleanupOrganizer.PostSongCleanup(guild).ConfigureAwait(false);
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

    private async Task SendAudioAsync(IGuild guild, Stream ffmpegStream, Stream pcmStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Copying music bytes to pcm stream");
            await ffmpegStream.CopyToAsync(pcmStream).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    public async Task SkipAudioAsync(IGuild guild)
    {
        await audioCleanupOrganizer.PostSongCleanup(guild).ConfigureAwait(false);
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
            audioLogger.LogWithGuildInfo(guild, "Setting _isPlaying to False");
            _isPlaying = false;
        }
    }

    private void SetToSongPlayingStatus(IGuild guild)
    {
        lock (IsPlayingLock)
        {
            audioLogger.LogWithGuildInfo(guild, "Setting _isPlaying to True");
            _isPlaying = true;
        }
    }
}