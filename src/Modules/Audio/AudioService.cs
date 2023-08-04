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

    public async Task JoinAudioAsync(IGuild guild, IVoiceChannel target)
    {
        if (_connectedAudioClients.TryGetValue(guild.Id, out _))
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
            _connectedAudioClients[guild.Id] = audioClient;
            Console.WriteLine($"Connected to voice on {guild.Name}.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task LeaveAudioAsync(IGuild guild)
    {
        if (!_connectedAudioClients.ContainsKey(guild.Id))
        {
            Console.WriteLine("Tried to remove bot from a guild where it is not connected to any voice channel");
            return;
        }

        try
        {
            await ExitWithDisposingAll(guild);
            Console.WriteLine($"Disconnected from voice on {guild.Name} and disposed of all streams");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task ExitWithDisposingAll(IGuild guild)
    {
        _connectedFfmpegProcesses.Remove(guild.Id, out var ffmpegProcess);
        ffmpegProcess?.Dispose();

        _connectedFfmpegStreams.Remove(guild.Id, out var ffmpegStream);
        ffmpegStream?.Dispose();

        _connectedPcmStreams.Remove(guild.Id, out var pcmStream);
        pcmStream?.Dispose();

        _connectedAudioClients.Remove(guild.Id, out var client);
        await (client?.StopAsync() ?? Task.CompletedTask);
        await (await guild.GetCurrentUserAsync()).ModifyAsync(x => x.Channel = null); // ???
        client?.Dispose();
    }

    public async Task SendAudioAsync(IGuild guild, string url)
    {
        if (_connectedAudioClients.TryGetValue(guild.Id, out var client))
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
            Process ffmpegProcess;
            Stream ffmpegStream;
            try
            {
                var createdProcess = CreateStream();

                if (createdProcess is null)
                {
                    Console.WriteLine("created stream for output was null");
                    return;
                }

                var baseStream = createdProcess.StandardOutput.BaseStream;

                ffmpegProcess = createdProcess;
                ffmpegStream = baseStream;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            _connectedFfmpegProcesses[guild.Id] = ffmpegProcess;
            _connectedFfmpegStreams[guild.Id] = ffmpegStream;

            Console.WriteLine($"Creating pcm stream of {url} in {guild.Name}");
            AudioOutStream pcmStream;
            try
            {
                pcmStream = client.CreatePCMStream(AudioApplication.Mixed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            _connectedPcmStreams[guild.Id] = pcmStream;

            try
            {
                Console.WriteLine($"Copying music bytes to pcm stream for {url} in {guild.Name}");
                await ffmpegStream.CopyToAsync(pcmStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine($"Flushing final bytes to pcm stream for {url} in {guild.Name}");
                try
                {
                    await pcmStream.FlushAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("failed to flush final bytes");
                    Console.WriteLine(e);
                }
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