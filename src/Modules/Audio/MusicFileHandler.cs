﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IMusicFileHandler
{
    Task DeleteMusicAsync(IGuild guild);
    Task<bool> DownloadMusicAsync(IGuild guild, string url);
    Process? CreateFfmpegProcess();
}

public sealed class MusicFileHandler : IMusicFileHandler
{
    private const string FileNameWithoutExtension = "music_file";
    private const string FileNameWithExtension = FileNameWithoutExtension + ".wav";

    private readonly IAudioLogger _audioLogger;

    public MusicFileHandler(IAudioLogger audioLogger)
    {
        _audioLogger = audioLogger;
    }

    public async Task DeleteMusicAsync(IGuild guild)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Deleting music file");
            File.Delete(GetFullPathToDownloadedFile());
            _audioLogger.LogWithGuildInfo(guild, "Deleted music file");
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    private static string GetFullPathToDownloadedFile()
    {
        var pathToAppContext = Path.GetFullPath(AppContext.BaseDirectory);
        var pathToFile = Path.Combine(pathToAppContext, FileNameWithExtension);
        return pathToFile;
    }

    public async Task<bool> DownloadMusicAsync(IGuild guild, string url)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Starting yt-dlp process");
            var process = Process.Start(new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "yt-dlp",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"--extract-audio --audio-format wav {url} -o {FileNameWithoutExtension}"
            });

            _audioLogger.LogWithGuildInfo(guild, "Waiting for yt-dlp process to finish download");
            await (process?.WaitForExitAsync() ?? Task.CompletedTask);
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }

        _audioLogger.LogWithGuildInfo(guild, "Downloaded music file");
        return true;
    }

    public Process? CreateFfmpegProcess() => Process.Start(new ProcessStartInfo
    {
        FileName = "ffmpeg",
        Arguments =
            $"-hide_banner -loglevel panic -i \"{GetFullPathToDownloadedFile()}\" -ac 2 -f s16le -ar 48000 pipe:1",
        UseShellExecute = false,
        RedirectStandardOutput = true
    });
}