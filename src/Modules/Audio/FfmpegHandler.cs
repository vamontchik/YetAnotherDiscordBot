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

public sealed class FfmpegHandler(
    IMusicFileHandler musicFileHandler,
    IAudioStore audioStore,
    IAudioLogger audioLogger)
    : IFfmpegHandler
{
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
            audioLogger.LogWithGuildInfo(guild, "Creating ffmpeg process");
            var createdProcess = musicFileHandler.CreateFfmpegProcess();
            if (createdProcess is null)
            {
                audioLogger.LogWithGuildInfo(guild, "createdProcess is null");
                return FfmpegCreationResult.Failed;
            }

            audioLogger.LogWithGuildInfo(guild, "Retrieving base stream");
            ffmpegProcess = createdProcess;
            ffmpegStream = createdProcess.StandardOutput.BaseStream;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            ffmpegProcess = null;
            ffmpegStream = null;
            return FfmpegCreationResult.Failed;
        }

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
               audioStore.AddFfmpegProcessForGuild(guild, ffmpegProcess) &&
               audioStore.AddFfmpegStreamForGuild(guild, ffmpegStream);
    }
}