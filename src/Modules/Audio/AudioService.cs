using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public class AudioService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedAudioClients = new();

    private readonly ConcurrentDictionary<ulong, Process> _connectedFfmpegProcesses = new();
    private readonly ConcurrentDictionary<ulong, Stream> _connectedFfmpegStreams = new();
    private readonly ConcurrentDictionary<ulong, AudioOutStream> _connectedPcmStreams = new();

    private const string FileNameWithoutExtension = "test";
    private const string FileNameWithExtension = FileNameWithoutExtension + ".wav";

    public async Task JoinAudioAsync(IGuild guild, IVoiceChannel voiceChannel)
    {
        if (_connectedAudioClients.TryGetValue(guild.Id, out _))
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Bot already is in a channel in this guild!");
            return;
        }

        var audioClient = await ConnectWithExceptionHandling(voiceChannel);
        if (audioClient is null)
            return;

        _connectedAudioClients[guild.Id] = audioClient;

        PrintWithGuildInfo(guild.Name, guild.Id.ToString(), $"Connected to voice on {guild.Name}.");
    }

    public async Task LeaveAudioAsync(IGuild guild)
    {
        if (!_connectedAudioClients.ContainsKey(guild.Id))
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(),
                "Tried to remove bot from a guild where it is not connected to any voice channel");
            return;
        }

        try
        {
            await ExitWithDisposingAll(guild);

            DeleteDownloadedFileWithExceptionHandling(guild);

            lock (InteractionWithIsPlayingLock)
            {
                _isPlaying = false;
            }

            PrintWithGuildInfo(guild.Name, guild.Id.ToString(),
                $"Disconnected from voice on {guild.Name} and disposed of all streams, process, and client");
        }
        catch (Exception e)
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(),
                "Unable to disconnect and dispose of all streams, process, and client");
            Console.WriteLine(e);
        }
    }

    private static readonly object InteractionWithIsPlayingLock = new();

    public async Task SendAudioAsync(IGuild guild, string url)
    {
        if (_connectedAudioClients.TryGetValue(guild.Id, out var client))
        {
            lock (InteractionWithIsPlayingLock)
            {
                if (IsPlayingSong())
                    return;

                if (!IsPlayingSong())
                    FlipIsPlayingStatus();
            }

            if (!await DownloadMusicWithExceptionHandling(url))
                return;

            if (!SetupFfmpegWithExceptionHandling(guild, url, out var ffmpegProcess, out var ffmpegStream))
                return;
            if (ffmpegProcess is null || ffmpegStream is null)
                return;

            var pcmStream = CreatePcmStreamWithExceptionHandling(url, guild, client);
            if (pcmStream is null)
                return;

            await SendAudioWithExceptionHandling(guild, url, ffmpegStream, pcmStream);

            FlipIsPlayingStatus();
        }
    }

    private async Task SendAudioWithExceptionHandling(
        IGuild guild,
        string url,
        Stream ffmpegStream,
        AudioOutStream pcmStream)
    {
        try
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(),
                $"Copying music bytes to pcm stream for {url} in {guild.Name}");
            await ffmpegStream.CopyToAsync(pcmStream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(),
                $"Flushing pcm stream for {url} in {guild.Name}");
            await FlushPcmStreamWithExceptionHandling(guild, pcmStream);

            await CleanupAfterSongEndsWithExceptionHandling(guild);

            DeleteDownloadedFileWithExceptionHandling(guild);
        }
    }

    private static async Task FlushPcmStreamWithExceptionHandling(IGuild guild, AudioOutStream pcmStream)
    {
        try
        {
            await pcmStream.FlushAsync();
        }
        catch (Exception e)
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Failed to flush final bytes");
            Console.WriteLine(e);
        }
    }

    private AudioOutStream? CreatePcmStreamWithExceptionHandling(string url, IGuild guild, IAudioClient client)
    {
        PrintWithGuildInfo(guild.Name, guild.Id.ToString(),
            $"Creating pcm stream of {url} in {guild.Name}");
        AudioOutStream pcmStream;
        try
        {
            pcmStream = client.CreatePCMStream(AudioApplication.Mixed);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DeleteDownloadedFileWithExceptionHandling(guild);
            return null;
        }

        _connectedPcmStreams[guild.Id] = pcmStream;
        return pcmStream;
    }

    private bool SetupFfmpegWithExceptionHandling(IGuild guild, string url,
        out Process? ffmpegProcess, out Stream? ffmpegStream)
    {
        PrintWithGuildInfo(guild.Name, guild.Id.ToString(),
            $"Creating ffmpeg stream of {url} in {guild.Name}");

        ffmpegProcess = null;
        ffmpegStream = null;

        try
        {
            var createdProcess = CreateFfmpegProcess();

            if (createdProcess is null)
            {
                PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "createdProcess was null");
                return false;
            }

            var baseStream = createdProcess.StandardOutput.BaseStream;

            ffmpegProcess = createdProcess;
            ffmpegStream = baseStream;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            ffmpegProcess = null;
            ffmpegStream = null;

            DeleteDownloadedFileWithExceptionHandling(guild);

            return false;
        }

        _connectedFfmpegProcesses[guild.Id] = ffmpegProcess;
        _connectedFfmpegStreams[guild.Id] = ffmpegStream;

        return true;
    }

    private static void DeleteDownloadedFileWithExceptionHandling(IGuild guild)
    {
        try
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Deleting local file");
            File.Delete(GetFullPathToDownloadedFile());
        }
        catch (Exception e)
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Unable to delete downloaded file");
            Console.WriteLine(e);
        }
    }

    private static async Task<bool> DownloadMusicWithExceptionHandling(string url)
    {
        try
        {
            await DownloadMusicFileAsync(url);
        }
        catch (Exception e)
        {
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

    private static Process? CreateFfmpegProcess()
    {
        var fullPath = GetFullPathToDownloadedFile();
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{fullPath}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        return Process.Start(startInfo);
    }

    private static string GetFullPathToDownloadedFile()
    {
        var pathToAppContext = Path.GetFullPath(AppContext.BaseDirectory);
        var pathToFile = Path.Combine(pathToAppContext, FileNameWithExtension);
        return pathToFile;
    }

    private static void PrintWithGuildInfo(string guildName, string guildId, string message) =>
        Console.WriteLine($"[{guildName}:{guildId}] {message}");

    private Task CleanupAfterSongEndsWithExceptionHandling(IGuild guild)
    {
        try
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Removing and disposing `ffmpegProcess`");
            _connectedFfmpegProcesses.Remove(guild.Id, out var ffmpegProcess);
            ffmpegProcess?.Dispose();

            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Removing and disposing `ffmpegStream`");
            _connectedFfmpegStreams.Remove(guild.Id, out var ffmpegStream);
            ffmpegStream?.Dispose();

            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Removing and disposing `pcmStream`");
            _connectedPcmStreams.Remove(guild.Id, out var pcmStream);
            pcmStream?.Dispose();

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Unable to fully cleanup");
            Console.WriteLine(e);

            return Task.CompletedTask;
        }
    }

    private async Task ExitWithDisposingAll(IGuild guild)
    {
        PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Removing and disposing `ffmpegProcess`");
        _connectedFfmpegProcesses.Remove(guild.Id, out var ffmpegProcess);
        ffmpegProcess?.Dispose();

        PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Removing and disposing `ffmpegStream`");
        _connectedFfmpegStreams.Remove(guild.Id, out var ffmpegStream);
        ffmpegStream?.Dispose();

        PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Removing and disposing `pcmStream`");
        _connectedPcmStreams.Remove(guild.Id, out var pcmStream);
        pcmStream?.Dispose();

        PrintWithGuildInfo(guild.Name, guild.Id.ToString(), "Removing, stopping, and disposing `audioClient`");
        _connectedAudioClients.Remove(guild.Id, out var audioClient);
        await (audioClient?.StopAsync() ?? Task.CompletedTask);
        await (await guild.GetCurrentUserAsync()).ModifyAsync(x => x.Channel = null); // ???
        audioClient?.Dispose();
    }

    private static async Task<IAudioClient?> ConnectWithExceptionHandling(IVoiceChannel voiceChannel)
    {
        try
        {
            return await voiceChannel.ConnectAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    private bool _isPlaying = false;
    private static readonly object IsPlayingLock = new();

    private void FlipIsPlayingStatus()
    {
        lock (IsPlayingLock)
        {
            _isPlaying = !_isPlaying;
        }
    }

    private bool IsPlayingSong()
    {
        lock (IsPlayingLock)
        {
            return _isPlaying;
        }
    }
}