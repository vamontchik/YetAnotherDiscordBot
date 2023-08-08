using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

// TODO: this class has too much logic, consider splitting it into several

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


    public async Task<(bool, string)> JoinAudioAsync(IGuild guildToConnectTo, IVoiceChannel voiceChannelToConnectTo) =>
        await _audioConnector.SafeConnectAsync(guildToConnectTo, voiceChannelToConnectTo);

    public async Task<(bool, string)> LeaveAudioAsync(IGuild guildToLeaveFrom)
    {
        var (disconnectSuccess, disconnectErrorMessage) =
            await _audioConnector.SafeDisconnectAsync(guildToLeaveFrom);
        var (musicFileDeletionSuccess, musicFileDeletionErrorMessage) =
            await MusicFileHandler.SafeDeleteMusicAsync(guildToLeaveFrom);

        if (disconnectSuccess && musicFileDeletionSuccess)
        {
            ResetPlayingStatusWithLock();
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
                ResetPlayingStatusWithLock();
                return (false, "Unable to find audio client");
            }

            audioClient = possibleAudioClient;
        }

        {
            var (success, errorMessage) = await MusicFileHandler.SafeDownloadMusicAsync(guild, url);
            if (!success)
            {
                ResetPlayingStatusWithLock();
                return (false, errorMessage);
            }
        }

        Stream ffmpegStream;
        {
            var (success, errorMessage) =
                await SafeSetupFfmpegAsync(guild, url, out var resultFfmpegProcess, out var resultFfmpegStream);

            if (!success)
            {
                ResetPlayingStatusWithLock();

                var (deletionSuccess, deletionErrorMessage) =
                    await MusicFileHandler.SafeDeleteMusicAsync(guild);

                var fullErrorMessage = deletionSuccess
                    ? errorMessage
                    : errorMessage + " => " + deletionErrorMessage;

                return (false, fullErrorMessage);
            }

            if (resultFfmpegProcess is null)
            {
                ResetPlayingStatusWithLock();

                const string newErrorMessage = "ffmpeg process is null";

                var (deletionSuccess, deletionErrorMessage) =
                    await MusicFileHandler.SafeDeleteMusicAsync(guild);

                var fullErrorMessage = deletionSuccess
                    ? newErrorMessage
                    : newErrorMessage + " => " + deletionErrorMessage;

                return (false, fullErrorMessage);
            }

            if (resultFfmpegStream is null)
            {
                ResetPlayingStatusWithLock();

                const string newErrorMessage = "ffmpeg stream is null";

                var (deletionSuccess, deletionErrorMessage) =
                    await MusicFileHandler.SafeDeleteMusicAsync(guild);

                var fullErrorMessage = deletionSuccess
                    ? newErrorMessage
                    : newErrorMessage + " => " + deletionErrorMessage;

                return (false, fullErrorMessage);
            }

            if (!_audioStore.AddFfmpegProcessForGuild(guild.Id, resultFfmpegProcess))
            {
                ResetPlayingStatusWithLock();

                var (deletionSuccess, deletionErrorMessage) =
                    await MusicFileHandler.SafeDeleteMusicAsync(guild);
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
                ResetPlayingStatusWithLock();

                var (ffmpegStreamCleanupSuccess, ffmpegStreamCleanupErrorMessage) =
                    await _audioDisposer.SafeCleanupFfmpegStreamAsync(guild);
                var (ffmpegProcessCleanupSuccess, ffmpegProcessCleanupErrorMessage) =
                    await _audioDisposer.SafeCleanupFfmpegProcessAsync(guild);
                var (deletionSuccess, deletionErrorMessage) =
                    await MusicFileHandler.SafeDeleteMusicAsync(guild);
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
            var (resultPcmStream, errorMessage) = await SafeCreatePcmStreamAsync(url, guild, audioClient);

            if (resultPcmStream is null)
            {
                ResetPlayingStatusWithLock();

                var (cleanupFfmpegStreamSuccess, cleanupFfmpegStreamErrorMessage) =
                    await _audioDisposer.SafeCleanupFfmpegStreamAsync(guild);
                var (cleanupFfmpegProcessSuccess, cleanupFfmpegProcessErrorMessage) =
                    await _audioDisposer.SafeCleanupFfmpegProcessAsync(guild);
                var (cleanupMusicFileSuccess, cleanupMusicFileErrorMessage) =
                    await MusicFileHandler.SafeDeleteMusicAsync(guild);

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
            var (success, errorMessage) = await SafeSendAudioAsync(guild, url, ffmpegStream, pcmStream);
            if (!success)
                return (false, errorMessage);
        }

        {
            ResetPlayingStatusWithLock();

            var (flushSuccess, flushErrorMessage) =
                await SafeFlushPcmStreamAsync(guild, url, pcmStream);
            var (cleanupSuccess, cleanupErrorMessage) =
                await SafeCleanupAfterSongEndsAsync(guild);
            var (musicFileCleanupSuccess, musicFileCleanupErrorMessage) =
                await MusicFileHandler.SafeDeleteMusicAsync(guild);

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

    private void ResetPlayingStatusWithLock()
    {
        lock (InteractionWithIsPlayingLock)
            SetToNoSongPlayingStatus();
    }

    private static Task<(bool, string)> SafeSetupFfmpegAsync(
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
            var createdProcess = MusicFileHandler.CreateFfmpegProcess();
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

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Created ffmpeg stream of {url} in {guildName}");

        return Task.FromResult((true, string.Empty));
    }

    private async Task<(AudioOutStream?, string)> SafeCreatePcmStreamAsync(
        string url,
        IGuild guild,
        IAudioClient client)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Creating pcm stream of {url} in {guildName}");

        AudioOutStream pcmStream;
        try
        {
            pcmStream = client.CreatePCMStream(AudioApplication.Mixed);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            const string baseErrorMessage = "Unable to create pcm stream";
            var (success, innerErrorMessage) = await MusicFileHandler.SafeDeleteMusicAsync(guild);
            var fullErrorMessage = success ? baseErrorMessage : innerErrorMessage + " => " + baseErrorMessage;
            return (null, fullErrorMessage);
        }

        if (!_audioStore.AddPcmStreamForGuild(guildId, pcmStream))
            return (null, "Unable to add pcm stream to internal storage");

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Created pcm stream of {url} in {guildName}");

        return (pcmStream, string.Empty);
    }

    private static async Task<(bool, string)> SafeSendAudioAsync(
        IGuild guild,
        string url,
        Stream ffmpegStream,
        AudioOutStream pcmStream)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Copying music bytes to pcm stream for {url} in {guildName}");

        try
        {
            await ffmpegStream.CopyToAsync(pcmStream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to copy (all?) bytes from ffmpeg stream to pcm stream");
        }

        return (true, string.Empty);
    }

    private static async Task<(bool, string)> SafeFlushPcmStreamAsync(
        IGuild guild,
        string url,
        AudioOutStream pcmStream)
    {
        var guildName = guild.Name;
        var guildIdStr = guild.Id.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
            $"Flushing pcm stream for {url} in {guildName}");

        try
        {
            await pcmStream.FlushAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Failed to flush final bytes");
        }

        return (true, string.Empty);
    }

    private async Task<(bool, string)> SafeCleanupAfterSongEndsAsync(IGuild guild)
    {
        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupPcmStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupFfmpegStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupFfmpegProcessAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        return (true, string.Empty);
    }

    public async Task<(bool, string)> SkipAudioAsync(IGuild guild)
    {
        ResetPlayingStatusWithLock();

        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupPcmStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupFfmpegStreamAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await _audioDisposer.SafeCleanupFfmpegProcessAsync(guild);
            if (!success)
                return (false, errorMessage);
        }

        {
            var (success, errorMessage) = await MusicFileHandler.SafeDeleteMusicAsync(guild);
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