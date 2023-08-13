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
    Task<(bool, string)> JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel);
    Task<(bool, string)> LeaveAudioAsync(IGuild guild);
    Task<(bool, string)> SendAudioAsync(IGuild guild, string url);
    Task<(bool, string)> SkipAudioAsync(IGuild guild);
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


    public async Task<(bool, string)> JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel) =>
        await _audioConnector.ConnectAsync(guild, voiceChannel);

    public async Task<(bool, string)> LeaveAudioAsync(IGuild guild)
    {
        var (disconnectSuccess, disconnectErrorMessage) =
            await _audioConnector.DisconnectAsync(guild);
        var (musicFileDeletionSuccess, musicFileDeletionErrorMessage) =
            await _musicFileHandler.DeleteMusicAsync(guild);

        if (disconnectSuccess && musicFileDeletionSuccess)
        {
            ResetPlayingStatusWithLock(guild);
            return (true, string.Empty);
        }

        var fullErrorMessage = string.Empty;
        if (!disconnectSuccess)
            fullErrorMessage += disconnectErrorMessage;
        if (!musicFileDeletionSuccess)
            fullErrorMessage += musicFileDeletionErrorMessage;

        return (false, fullErrorMessage);
    }

    public async Task<(bool, string)> SendAudioAsync(IGuild guild, string url)
    {
        lock (InteractionWithIsPlayingLock)
        {
            if (IsPlayingSong())
                return (false, "Currently playing a song");

            SetToSongPlayingStatus();
        }

        IAudioClient audioClient;
        {
            var possibleAudioClient = _audioStore.GetAudioClientForGuild(guild.Id);
            if (possibleAudioClient is null)
            {
                ResetPlayingStatusWithLock(guild);
                return (false, "Unable to find audio client");
            }

            audioClient = possibleAudioClient;
        }

        {
            var (success, errorMessage) = await _musicFileHandler.DownloadMusicAsync(guild, url);
            if (!success)
            {
                ResetPlayingStatusWithLock(guild);
                return (false, errorMessage);
            }
        }

        Stream ffmpegStream;
        {
            var (success, errorMessage) =
                await SetupFfmpegAsync(guild, url, out var resultFfmpegProcess, out var resultFfmpegStream);

            if (!success)
            {
                ResetPlayingStatusWithLock(guild);

                var (deletionSuccess, deletionErrorMessage) =
                    await _musicFileHandler.DeleteMusicAsync(guild);

                var fullErrorMessage = deletionSuccess
                    ? errorMessage
                    : errorMessage + " => " + deletionErrorMessage;

                return (false, fullErrorMessage);
            }

            if (resultFfmpegProcess is null)
            {
                ResetPlayingStatusWithLock(guild);

                const string newErrorMessage = "ffmpeg process is null";

                var (deletionSuccess, deletionErrorMessage) =
                    await _musicFileHandler.DeleteMusicAsync(guild);

                var fullErrorMessage = deletionSuccess
                    ? newErrorMessage
                    : newErrorMessage + " => " + deletionErrorMessage;

                return (false, fullErrorMessage);
            }

            if (resultFfmpegStream is null)
            {
                ResetPlayingStatusWithLock(guild);

                const string newErrorMessage = "ffmpeg stream is null";

                var (deletionSuccess, deletionErrorMessage) =
                    await _musicFileHandler.DeleteMusicAsync(guild);

                var fullErrorMessage = deletionSuccess
                    ? newErrorMessage
                    : newErrorMessage + " => " + deletionErrorMessage;

                return (false, fullErrorMessage);
            }

            if (!_audioStore.AddFfmpegProcessForGuild(guild.Id, resultFfmpegProcess))
            {
                ResetPlayingStatusWithLock(guild);

                var (deletionSuccess, deletionErrorMessage) =
                    await _musicFileHandler.DeleteMusicAsync(guild);
                var removalSuccess = _audioStore.RemoveFfmpegProcessFromGuild(guild.Id, out _);

                var fullErrorMessage = "Unable to add ffmpeg process to internal storage";
                if (!deletionSuccess)
                    fullErrorMessage += " => " + deletionErrorMessage;
                if (!removalSuccess)
                    fullErrorMessage += " => " + "Unable to remove ffmpeg process from internal storage";

                return (false, fullErrorMessage);
            }

            if (!_audioStore.AddFfmpegStreamForGuild(guild.Id, resultFfmpegStream))
            {
                ResetPlayingStatusWithLock(guild);

                var (ffmpegStreamCleanupSuccess, ffmpegStreamCleanupErrorMessage) =
                    await _audioDisposer.CleanupFfmpegStreamAsync(guild);
                var (ffmpegProcessCleanupSuccess, ffmpegProcessCleanupErrorMessage) =
                    await _audioDisposer.CleanupFfmpegProcessAsync(guild);
                var (deletionSuccess, deletionErrorMessage) =
                    await _musicFileHandler.DeleteMusicAsync(guild);
                var removalSuccess = _audioStore.RemoveFfmpegProcessFromGuild(guild.Id, out _);

                var fullErrorMessage = "Unable to add ffmpeg stream to internal storage";
                if (!ffmpegStreamCleanupSuccess)
                    fullErrorMessage += " => " + ffmpegStreamCleanupErrorMessage;
                if (!ffmpegProcessCleanupSuccess)
                    fullErrorMessage += " => " + ffmpegProcessCleanupErrorMessage;
                if (!deletionSuccess)
                    fullErrorMessage += " => " + deletionErrorMessage;
                if (!removalSuccess)
                    fullErrorMessage += " => " + "Unable to remove ffmpeg process from internal storage";

                return (false, fullErrorMessage);
            }

            ffmpegStream = resultFfmpegStream;
        }

        AudioOutStream pcmStream;
        {
            var (resultPcmStream, errorMessage) = await CreatePcmStreamAsync(url, guild, audioClient);

            if (resultPcmStream is null)
            {
                ResetPlayingStatusWithLock(guild);

                var (cleanupFfmpegStreamSuccess, cleanupFfmpegStreamErrorMessage) =
                    await _audioDisposer.CleanupFfmpegStreamAsync(guild);
                var (cleanupFfmpegProcessSuccess, cleanupFfmpegProcessErrorMessage) =
                    await _audioDisposer.CleanupFfmpegProcessAsync(guild);
                var (cleanupMusicFileSuccess, cleanupMusicFileErrorMessage) =
                    await _musicFileHandler.DeleteMusicAsync(guild);

                var fullErrorMessage = errorMessage;
                if (!cleanupFfmpegStreamSuccess)
                    fullErrorMessage += " => " + cleanupFfmpegStreamErrorMessage;
                if (!cleanupFfmpegProcessSuccess)
                    fullErrorMessage += " => " + cleanupFfmpegProcessErrorMessage;
                if (!cleanupMusicFileSuccess)
                    fullErrorMessage += " => " + cleanupMusicFileErrorMessage;

                return (false, fullErrorMessage);
            }

            pcmStream = resultPcmStream;
        }

        {
            var (success, errorMessage) = await SendAudioAsync(guild, url, ffmpegStream, pcmStream);
            if (!success)
                return (false, errorMessage);
        }

        {
            ResetPlayingStatusWithLock(guild);

            var (flushSuccess, flushErrorMessage) =
                await FlushPcmStreamAsync(guild, url, pcmStream);
            var (cleanupSuccess, cleanupErrorMessage) =
                await CleanupAfterSongEndsAsync(guild);
            var (musicFileCleanupSuccess, musicFileCleanupErrorMessage) =
                await _musicFileHandler.DeleteMusicAsync(guild);

            if (flushSuccess && cleanupSuccess && musicFileCleanupSuccess)
                return (true, string.Empty);

            var fullErrorMessage = string.Empty;
            if (!flushSuccess)
                fullErrorMessage += " => " + flushErrorMessage;
            if (!cleanupSuccess)
                fullErrorMessage += " => " + cleanupErrorMessage;
            if (!musicFileCleanupSuccess)
                fullErrorMessage += " => " + musicFileCleanupErrorMessage;

            return (false, fullErrorMessage);
        }
    }

    private void ResetPlayingStatusWithLock(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: about to obtain lock");
        lock (InteractionWithIsPlayingLock)
        {
            _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: obtained lock");
            SetToNoSongPlayingStatus();
            _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: reset status");
        }

        _audioLogger.LogWithGuildInfo(guild, "ResetPlayingStatusWithLock: released lock");
    }

    private Task<(bool, string)> SetupFfmpegAsync(
        IGuild guild,
        string url,
        out Process? ffmpegProcess,
        out Stream? ffmpegStream)
    {
        _audioLogger.LogWithGuildInfo(guild, $"Creating ffmpeg stream of {url} in {guild.Name}");

        ffmpegProcess = null;
        ffmpegStream = null;

        try
        {
            var createdProcess = _musicFileHandler.CreateFfmpegProcess();
            if (createdProcess is null)
                return Task.FromResult((false, "createdProcess was null"));

            var baseStream = createdProcess.StandardOutput.BaseStream;

            ffmpegProcess = createdProcess;
            ffmpegStream = baseStream;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            ffmpegProcess = null;
            ffmpegStream = null;

            return Task.FromResult((false, "Exception occured while creating ffmpeg process"));
        }

        _audioLogger.LogWithGuildInfo(guild, $"Created ffmpeg stream of {url} in {guild.Name}");

        return Task.FromResult((true, string.Empty));
    }

    private async Task<(AudioOutStream?, string)> CreatePcmStreamAsync(
        string url,
        IGuild guild,
        IAudioClient client)
    {
        _audioLogger.LogWithGuildInfo(guild, $"Creating pcm stream of {url} in {guild.Name}");

        AudioOutStream pcmStream;
        try
        {
            pcmStream = client.CreatePCMStream(AudioApplication.Mixed);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            const string baseErrorMessage = "Unable to create pcm stream";
            var (success, innerErrorMessage) = await _musicFileHandler.DeleteMusicAsync(guild);
            var fullErrorMessage = success ? baseErrorMessage : innerErrorMessage + " => " + baseErrorMessage;
            return (null, fullErrorMessage);
        }

        if (!_audioStore.AddPcmStreamForGuild(guild.Id, pcmStream))
            return (null, "Unable to add pcm stream to internal storage");

        _audioLogger.LogWithGuildInfo(guild, $"Created pcm stream of {url} in {guild.Name}");

        return (pcmStream, string.Empty);
    }

    private async Task<(bool, string)> SendAudioAsync(
        IGuild guild,
        string url,
        Stream ffmpegStream,
        Stream pcmStream)
    {
        _audioLogger.LogWithGuildInfo(guild, $"Copying music bytes to pcm stream for {url} in {guild.Name}");

        try
        {
            await ffmpegStream.CopyToAsync(pcmStream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to copy (all?) bytes from ffmpeg stream to pcm stream");
        }

        _audioLogger.LogWithGuildInfo(guild, $"Copied music bytes to pcm stream for {url} in {guild.Name}");

        return (true, string.Empty);
    }

    private async Task<(bool, string)> FlushPcmStreamAsync(
        IGuild guild,
        string url,
        Stream pcmStream)
    {
        _audioLogger.LogWithGuildInfo(guild, $"Flushing pcm stream for {url} in {guild.Name}");

        try
        {
            await pcmStream.FlushAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Failed to flush final bytes");
        }

        _audioLogger.LogWithGuildInfo(guild, $"Flushed pcm stream for {url} in {guild.Name}");

        return (true, string.Empty);
    }

    private async Task<(bool, string)> CleanupAfterSongEndsAsync(IGuild guild)
    {
        {
            var (success, errorMessage) = await _audioDisposer.CleanupPcmStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.CleanupFfmpegStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.CleanupFfmpegProcessAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        return (true, string.Empty);
    }

    public async Task<(bool, string)> SkipAudioAsync(IGuild guild)
    {
        ResetPlayingStatusWithLock(guild);

        {
            var (success, errorMessage) = await _audioDisposer.CleanupPcmStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.CleanupFfmpegStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.CleanupFfmpegProcessAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _musicFileHandler.DeleteMusicAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        return (true, string.Empty);
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