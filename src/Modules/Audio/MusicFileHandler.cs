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

    public static Task<bool> SafeDeleteMusicAsync(IGuild guild)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        try
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Deleting music file");
            var fullPathToDownloadedFile = GetFullPathToDownloadedFile();
            File.Delete(fullPathToDownloadedFile);
        }
        catch (Exception e)
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Unable to delete music file");
            Console.WriteLine(e);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public static async Task<bool> SafeDownloadMusicAsync(IGuild guild, string url)
    {
        var guildName = guild.Name;
        var guildId = guild.Id;
        var guildIdStr = guildId.ToString();

        try
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Downloading music file");
            await DownloadMusicFileAsync(url);
        }
        catch (Exception e)
        {
            AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Unable to download music file");
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    private static async Task DownloadMusicFileAsync(string url)
    {
        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            FileName = "yt-dlp",
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = $"--extract-audio --audio-format wav {url} -o {FileNameWithoutExtension}"
        };
        using var process = Process.Start(startInfo);
        if (process is null)
            throw new Exception("Unable to create process for ffmpeg");
        await process.WaitForExitAsync();
    }

    public static string GetFullPathToDownloadedFile()
    {
        var pathToAppContext = Path.GetFullPath(AppContext.BaseDirectory);
        var pathToFile = Path.Combine(pathToAppContext, FileNameWithExtension);
        return pathToFile;
    }
}