using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IMusicFileHandler
{
    Task<(bool, string)> DeleteMusicAsync(IGuild guild);
    Task<(bool, string)> DownloadMusicAsync(IGuild guild, string url);
    Process? CreateFfmpegProcess();
}

public class MusicFileHandler : IMusicFileHandler
{
    private const string FileNameWithoutExtension = "music_file";
    private const string FileNameWithExtension = FileNameWithoutExtension + ".wav";

    private readonly IAudioLogger _audioLogger;

    public MusicFileHandler(IAudioLogger audioLogger)
    {
        _audioLogger = audioLogger;
    }

    public Task<(bool, string)> DeleteMusicAsync(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, "Deleting music file");

        try
        {
            File.Delete(GetFullPathToDownloadedFile());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            const string errorMessage = "Unable to delete music file";
            return Task.FromResult((false, errorMessage));
        }

        _audioLogger.LogWithGuildInfo(guild, "Deleted music file");

        return Task.FromResult((true, string.Empty));
    }

    public async Task<(bool, string)> DownloadMusicAsync(IGuild guild, string url)
    {
        _audioLogger.LogWithGuildInfo(guild, "Downloading music file");

        Process nonNullProcess;
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "yt-dlp",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"--extract-audio --audio-format wav {url} -o {FileNameWithoutExtension}"
            });

            nonNullProcess = process ?? throw new Exception();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to start download process");
        }

        try
        {
            await nonNullProcess.WaitForExitAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Exception occured while waiting for download process to finish");
        }

        _audioLogger.LogWithGuildInfo(guild, "Downloaded music file");

        return (true, string.Empty);
    }

    public Process? CreateFfmpegProcess() => Process.Start(new ProcessStartInfo
    {
        FileName = "ffmpeg",
        Arguments =
            $"-hide_banner -loglevel panic -i \"{GetFullPathToDownloadedFile()}\" -ac 2 -f s16le -ar 48000 pipe:1",
        UseShellExecute = false,
        RedirectStandardOutput = true
    });

    private static string GetFullPathToDownloadedFile()
    {
        var pathToAppContext = Path.GetFullPath(AppContext.BaseDirectory);
        var pathToFile = Path.Combine(pathToAppContext, FileNameWithExtension);
        return pathToFile;
    }
}