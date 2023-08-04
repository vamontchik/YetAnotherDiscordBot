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
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();

    public async Task JoinAudioAsync(IGuild guild, IVoiceChannel target)
    {
        if (_connectedChannels.TryGetValue(guild.Id, out _))
        {
            Console.WriteLine("Bot already is in a channel in this guild!");
            return;
        }

        IAudioClient? audioClient = null;
        try
        {
            audioClient = await target.ConnectAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (audioClient is null)
            return;

        try
        {
            _connectedChannels[guild.Id] = audioClient;
            Console.WriteLine($"Connected to voice on {guild.Name}.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task LeaveAudioAsync(IGuild guild)
    {
        if (!_connectedChannels.ContainsKey(guild.Id))
        {
            Console.WriteLine("Tried to remove bot from a guild where it is not connected to any voice channel");
            return;
        }

        var client = _connectedChannels[guild.Id];

        try
        {
            await client.StopAsync();
            await (await guild.GetCurrentUserAsync()).ModifyAsync(x => x.Channel = null); // ???
            client.Dispose();
            Console.WriteLine($"Disconnected from voice on {guild.Name}.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        _connectedChannels.Remove(guild.Id, out _);
    }

    public async Task SendAudioAsync(IGuild guild, string url)
    {
        if (_connectedChannels.TryGetValue(guild.Id, out var client))
        {
            if (!await DownloadMusicFileAsync(url))
                return;

            if (!CreateFfmpegStream(url, guild.Name, out var process, out var output))
                return;
            if (process is null || output is null)
            {
                Console.WriteLine("unable to create ffmpeg stream: `process` or `output` is null");
                return;
            }

            if (CreatePcmStream(url, guild.Name, client, out var stream))
                return;
            if (stream is null)
            {
                Console.WriteLine("unable to create pcm stream: `stream` is null");
                return;
            }

            await SendMusic(guild, url, output, stream);

            await DisposeStreamsAndProcesses(stream, output, process);
        }
    }

    private static async Task DisposeStreamsAndProcesses(
        AudioOutStream stream,
        Stream output,
        Process process)
    {
        Console.WriteLine("Disposing of all streams");
        try
        {
            await stream.DisposeAsync();
            await output.DisposeAsync();
            process.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine("failed to dispose of all streams");
            Console.WriteLine(e);
        }
    }

    private static async Task SendMusic(
        IGuild guild,
        string url,
        Stream output,
        AudioOutStream stream)
    {
        try
        {
            Console.WriteLine($"Copying music bytes to ffmpeg stream for {url} in {guild.Name}");
            await output.CopyToAsync(stream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            Console.WriteLine($"Flushing final bytes to ffmpeg stream for {url} in {guild.Name}");
            try
            {
                await stream.FlushAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("failed to flush final bytes");
                Console.WriteLine(e);
            }
        }
    }

    private static bool CreatePcmStream(
        string url,
        string guildName,
        IAudioClient client,
        out AudioOutStream? stream)
    {
        Console.WriteLine($"Creating pcm stream of {url} in {guildName}");

        stream = null;

        try
        {
            stream = client.CreatePCMStream(AudioApplication.Mixed);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    private static bool CreateFfmpegStream(
        string url,
        string guildName,
        out Process? process,
        out Stream? output)
    {
        Console.WriteLine($"Creating ffmpeg stream of {url} in {guildName}");

        process = null;
        output = null;

        try
        {
            var createdProcess = CreateStream();

            if (createdProcess is null)
            {
                Console.WriteLine("created stream for output was null");
                return false;
            }

            var baseStream = createdProcess.StandardOutput.BaseStream;

            process = createdProcess;
            output = baseStream;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    private static async Task<bool> DownloadMusicFileAsync(string url)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "yt-dlp",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"--extract-audio --audio-format wav {url} -o test"
            };
            using var process = Process.Start(startInfo);
            if (process is null)
                throw new Exception("unable to create process for ffmpeg");
            await process.WaitForExitAsync();
            process.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    private static Process? CreateStream()
    {
        var pathToAppContext = Path.GetFullPath(AppContext.BaseDirectory);
        var pathToFile = Path.Combine(pathToAppContext, "test.wav");
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{pathToFile}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        return Process.Start(startInfo);
    }
}