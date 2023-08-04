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
            return;

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
            try
            {
                await DownloadMusicFileAsync(url);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            Console.WriteLine($"Creating ffmpeg stream of {url} in {guild.Name}");
            Process process;
            Stream output;
            try
            {
                var createdProcess = CreateStream();

                if (createdProcess is null)
                {
                    Console.WriteLine("created stream for output was null");
                    return;
                }

                var baseStream = createdProcess.StandardOutput.BaseStream;

                process = createdProcess;
                output = baseStream;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            Console.WriteLine($"Creating pcm stream of {url} in {guild.Name}");
            AudioOutStream stream;
            try
            {
                stream = client.CreatePCMStream(AudioApplication.Mixed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

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
                await stream.FlushAsync();
                Console.WriteLine("Disposing various processes and streams");
                await stream.DisposeAsync();
                await output.DisposeAsync();
                process.Dispose();
            }
        }
    }

    private static async Task DownloadMusicFileAsync(string url)
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