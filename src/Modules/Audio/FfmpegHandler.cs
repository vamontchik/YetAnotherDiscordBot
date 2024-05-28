using System;
using System.Diagnostics;
using System.IO;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IFfmpegHandler
{
    public FfmpegHandler.FfmpegCreationResult SetupFfmpeg(
        IGuild guild,
        string url,
        out Process? ffmpegProcess,
        out Stream? ffmpegStream);

    public bool CheckAndStore(
        IGuild guild,
        FfmpegHandler.FfmpegCreationResult result,
        Process? ffmpegProcess,
        Stream? ffmpegStream);
}

public class FfmpegHandler : IFfmpegHandler
{
    private readonly IMusicFileHandler _musicFileHandler;
    private readonly IAudioStore _audioStore;
    private readonly IAudioLogger _audioLogger;

    public FfmpegHandler(
        IMusicFileHandler musicFileHandler,
        IAudioStore audioStore,
        IAudioLogger audioLogger)
    {
        _musicFileHandler = musicFileHandler;
        _audioStore = audioStore;
        _audioLogger = audioLogger;
    }

    public enum FfmpegCreationResult
    {
        Successful,
        Failed
    }

    public FfmpegCreationResult SetupFfmpeg(
        IGuild guild,
        string url,
        out Process? ffmpegProcess,
        out Stream? ffmpegStream)
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
                return FfmpegCreationResult.Failed;
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
            return FfmpegCreationResult.Failed;
        }

        _audioLogger.LogWithGuildInfo(guild, $"Created ffmpeg stream of {url} in {guild.Name}");
        return FfmpegCreationResult.Successful;
    }

    public bool CheckAndStore(
        IGuild guild,
        FfmpegCreationResult result,
        Process? ffmpegProcess,
        Stream? ffmpegStream)
    {
        return result != FfmpegCreationResult.Failed &&
               ffmpegProcess is not null &&
               ffmpegStream is not null &&
               _audioStore.AddFfmpegProcessForGuild(guild, ffmpegProcess) &&
               _audioStore.AddFfmpegStreamForGuild(guild, ffmpegStream);
    }
}