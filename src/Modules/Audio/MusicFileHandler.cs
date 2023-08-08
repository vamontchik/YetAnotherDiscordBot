using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

public static class MusicFileHandler
{
    private const string FileNameWithoutExtension = "music_file";
    private const string FileNameWithExtension = FileNameWithoutExtension + ".wav";

    public static Task<(bool, string)> SafeDeleteMusicAsync(IGuild guild)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Deleting music file");

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

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Deleted music file");

        return Task.FromResult((true, string.Empty));
    }

    public static async Task<(bool, string)> SafeDownloadMusicAsync(IGuild guild, string url)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Downloading music file");

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

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Downloaded music file");

        return (true, string.Empty);
    }

    public static Process? CreateFfmpegProcess() => Process.Start(new ProcessStartInfo
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