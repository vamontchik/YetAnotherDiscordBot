using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public interface IPcmStreamHandler
{
    Task<AudioOutStream?> CreatePcmStreamAsync(
        string url,
        IGuild guild,
        IAudioClient client);

    Task FlushPcmStreamAsync(
        IGuild guild,
        string url,
        Stream pcmStream);
}

public class PcmStreamHandler : IPcmStreamHandler
{
    private readonly IAudioStore _audioStore;
    private readonly IAudioLogger _audioLogger;

    public PcmStreamHandler(
        IAudioStore audioStore,
        IAudioLogger audioLogger)
    {
        _audioStore = audioStore;
        _audioLogger = audioLogger;
    }
    
    public Task<AudioOutStream?> CreatePcmStreamAsync(
        string url,
        IGuild guild,
        IAudioClient client)
    {
        AudioOutStream pcmStream;
        try
        {
            _audioLogger.LogWithGuildInfo(guild, $"Creating pcm stream of {url} in {guild.Name}");
            pcmStream = client.CreatePCMStream(AudioApplication.Mixed);
            _audioLogger.LogWithGuildInfo(guild, $"Created pcm stream of {url} in {guild.Name}");
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            return Task.FromResult<AudioOutStream?>(null);
        }

        return _audioStore.AddPcmStreamForGuild(guild, pcmStream)
            ? Task.FromResult<AudioOutStream?>(pcmStream)
            : Task.FromResult<AudioOutStream?>(null);
    }
    
    public async Task FlushPcmStreamAsync(
        IGuild guild,
        string url,
        Stream pcmStream)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, $"Flushing pcm stream for {url} in {guild.Name}");
            await pcmStream.FlushAsync();
            _audioLogger.LogWithGuildInfo(guild, $"Flushed pcm stream for {url} in {guild.Name}");
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }
}