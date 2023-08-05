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
        await _audioConnector.ConnectAsync(guildToConnectTo, voiceChannelToConnectTo);

    public async Task<bool> LeaveAudioAsync(IGuild guildToLeaveFrom)
    {
        var didDisconnectSucceed = await _audioConnector.DisconnectAsync(guildToLeaveFrom);
        var didDeletionSucceed = await MusicFileHandler.SafeDeleteMusicAsync(guildToLeaveFrom);
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

        if (!await MusicFileHandler.SafeDownloadMusicAsync(guild, url))
        {
            ResetPlayingStatusWithLock();
            return false;
        }

        if (!await SetupFfmpegWithExceptionHandling(guild, url, out var ffmpegProcess, out var ffmpegStream))
        {
            _ = await MusicFileHandler.SafeDeleteMusicAsync(guild); // TODO: return value?
            ResetPlayingStatusWithLock();
            return false;
        }

        if (ffmpegProcess is null || ffmpegStream is null)
        {
            _ = await MusicFileHandler.SafeDeleteMusicAsync(guild); // TODO: return value ?
            ResetPlayingStatusWithLock();
            return false;
        }

        var pcmStream = await CreatePcmStreamWithExceptionHandlingAsync(url, guild, audioClient);
        if (pcmStream is null)
        {
            _ = await _audioDisposer.CleanupFfmpegProcessAsync(guild); // TODO: return value ?
            _ = await _audioDisposer.CleanupFfmpegStreamAsync(guild); // TODO: return value ?
            _ = await MusicFileHandler.SafeDeleteMusicAsync(guild); // TODO: return value ?
            ResetPlayingStatusWithLock();
            return false;
        }

        if (!await SendAudioWithExceptionHandlingAsync(guild, url, ffmpegStream, pcmStream))
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

        Task<bool> SetupFfmpegWithExceptionHandling(
            IGuild innerGuild,
            string innerUrl,
            out Process? innerFfmpegProcess,
            out Stream? innerFfmpegStream)
        {
            var guildName = innerGuild.Name;
            var guildId = innerGuild.Id;
            var guildIdStr = guildId.ToString();

            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                $"Creating ffmpeg stream of {innerUrl} in {guildName}");

            innerFfmpegProcess = null;
            innerFfmpegStream = null;

            try
            {
                var createdProcess = CreateFfmpegProcess();

                if (createdProcess is null)
                {
                    AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "createdProcess was null");
                    return Task.FromResult(false);
                }

                var baseStream = createdProcess.StandardOutput.BaseStream;

                innerFfmpegProcess = createdProcess;
                innerFfmpegStream = baseStream;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                innerFfmpegProcess = null;
                innerFfmpegStream = null;

                return Task.FromResult(false);
            }

            _audioStore.AddFfmpegProcessForGuild(guildId, innerFfmpegProcess);
            _audioStore.AddFfmpegStreamForGuild(guildId, innerFfmpegStream);

            return Task.FromResult(true);

            static Process? CreateFfmpegProcess()
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
        }

        async Task<AudioOutStream?> CreatePcmStreamWithExceptionHandlingAsync(
            string innerUrl,
            IGuild innerGuild,
            IAudioClient client)
        {
            var guildName = innerGuild.Name;
            var guildId = innerGuild.Id;
            var guildIdStr = guildId.ToString();

            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                $"Creating pcm stream of {innerUrl} in {guildName}");

            AudioOutStream innerPcmStream;
            try
            {
                innerPcmStream = client.CreatePCMStream(AudioApplication.Mixed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _ = await MusicFileHandler.SafeDeleteMusicAsync(innerGuild); // TODO: return value?
                return null;
            }

            _audioStore.AddPcmStreamForGuild(guildId, innerPcmStream);

            return innerPcmStream;
        }

        async Task<bool> SendAudioWithExceptionHandlingAsync(
            IGuild innerGuild,
            string innerUrl,
            Stream innerFfmpegStream,
            AudioOutStream innerPcmStream)
        {
            var guildName = innerGuild.Name;
            var guildId = innerGuild.Id;
            var guildIdStr = guildId.ToString();

            bool didFlushSucceed;
            bool didCleanupSucceed;
            bool didFileDeletionSucceed;
            try
            {
                AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                    $"Copying music bytes to pcm stream for {innerUrl} in {guildName}");
                await innerFfmpegStream.CopyToAsync(innerPcmStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                AudioLogger.PrintWithGuildInfo(guildName, guildIdStr,
                    $"Flushing pcm stream for {innerUrl} in {guildName}");

                didFlushSucceed = await FlushPcmStreamWithExceptionHandlingAsync(innerGuild, innerPcmStream);
                didCleanupSucceed = await CleanupAfterSongEndsWithExceptionHandlingAsync(innerGuild);
                didFileDeletionSucceed = await MusicFileHandler.SafeDeleteMusicAsync(innerGuild);
            }

            return didFlushSucceed &&
                   didCleanupSucceed &&
                   didFileDeletionSucceed;

            static async Task<bool> FlushPcmStreamWithExceptionHandlingAsync(IGuild guild, AudioOutStream pcmStream)
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

            async Task<bool> CleanupAfterSongEndsWithExceptionHandlingAsync(IGuild innerGuild2)
            {
                var innerGuildName = innerGuild2.Name;
                var innerGuildId = innerGuild2.Id;
                var innerGuildIdStr = innerGuildId.ToString();

                bool didFfmpegProcessCleanupSucceed;
                bool didFfmpegStreamCleanupSucceed;
                bool didPcmStreamCleanupSucceed;
                try
                {
                    didFfmpegProcessCleanupSucceed = await _audioDisposer.CleanupFfmpegProcessAsync(innerGuild2);
                    didFfmpegStreamCleanupSucceed = await _audioDisposer.CleanupFfmpegStreamAsync(innerGuild2);
                    didPcmStreamCleanupSucceed = await _audioDisposer.CleanupPcmStream(innerGuild2);
                }
                catch (Exception e)
                {
                    AudioLogger.PrintWithGuildInfo(innerGuildName, innerGuildIdStr, "Unable to fully cleanup");
                    Console.WriteLine(e);

                    return false;
                }

                return didFfmpegProcessCleanupSucceed &&
                       didFfmpegStreamCleanupSucceed &&
                       didPcmStreamCleanupSucceed;
            }
        }
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