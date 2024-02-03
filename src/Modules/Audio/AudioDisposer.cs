using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

public interface IAudioDisposer
{
    Task CleanupFfmpegProcessAsync(IGuild guild);
    Task CleanupFfmpegStreamAsync(IGuild guild);
    Task CleanupPcmStreamAsync(IGuild guild);
    Task CleanupAudioClientAsync(IGuild guild);
}

public sealed class AudioDisposer : IAudioDisposer
{
    private readonly IAudioStore _audioStore;
    private readonly IAudioLogger _audioLogger;

    public AudioDisposer(
        IAudioStore audioStore,
        IAudioLogger audioLogger)
    {
        _audioStore = audioStore;
        _audioLogger = audioLogger;
    }

    #region CleanupFfmpegProcessAsync

    public async Task CleanupFfmpegProcessAsync(IGuild guild)
    {
        if (!RemoveStoredFfmpegProcess(guild, out var ffmpegProcess) || ffmpegProcess is null)
            return;

        if (!DisposeProcess(guild, ffmpegProcess))
            return;

        _audioLogger.LogWithGuildInfo(guild, "Removed and disposed of ffmpeg process");
    }

    private bool RemoveStoredFfmpegProcess(IGuild guild, out Process? ffmpegProcess) =>
        _audioStore.RemoveFfmpegProcessFromGuild(guild, out ffmpegProcess);

    private bool DisposeProcess(IGuild guild, IDisposable ffmpegProcess)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Disposing of ffmpeg process");
            ffmpegProcess.Dispose();
            return true;
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    #endregion

    #region CleanupFfmpegStreamAsync

    public async Task CleanupFfmpegStreamAsync(IGuild guild)
    {
        if (!RemoveStoredFfmpegStream(guild, out var ffmpegStream) || ffmpegStream is null)
            return;

        if (!await DisposeFfmpegStream(guild, ffmpegStream))
            return;

        _audioLogger.LogWithGuildInfo(guild, "Removed and disposed of ffmpeg stream");
    }

    private bool RemoveStoredFfmpegStream(IGuild guild, out Stream? ffmpegStream) =>
        _audioStore.RemoveFfmpegStreamFromGuild(guild, out ffmpegStream);

    private async Task<bool> DisposeFfmpegStream(IGuild guild, IAsyncDisposable ffmpegStream)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Disposing of ffmpeg stream");
            await ffmpegStream.DisposeAsync();
            return true;
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    #endregion

    #region CleanupPcmStreamAsync

    public async Task CleanupPcmStreamAsync(IGuild guild)
    {
        if (!RemoveStoredPcmStream(guild, out var pcmStream) || pcmStream is null)
            return;

        if (!await DisposePcmStream(guild, pcmStream))
            return;

        _audioLogger.LogWithGuildInfo(guild, "Removed and disposed of pcm stream");
    }

    private bool RemoveStoredPcmStream(IGuild guild, out AudioOutStream? pcmStream) =>
        _audioStore.RemovePcmStreamFromGuild(guild, out pcmStream);

    private async Task<bool> DisposePcmStream(IGuild guild, IAsyncDisposable pcmStream)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Disposing of pcm stream");
            await pcmStream.DisposeAsync();
            return true;
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    #endregion

    #region CleanupAudioClientAsync

    public async Task CleanupAudioClientAsync(IGuild guild)
    {
        if (!RemoveStoredAudioClient(guild, out var audioClient) || audioClient is null)
            return;

        await StopAudioClient(guild, audioClient);
        await EraseChannelPropertyOfCurrentUser(guild);
        DisposeAudioClient(guild, audioClient);

        _audioLogger.LogWithGuildInfo(guild, "Removed, stopped, and disposed of audio client");
    }

    private async Task EraseChannelPropertyOfCurrentUser(IGuild guild)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Obtaining current user");
            var currentUser = await guild.GetCurrentUserAsync();
            _audioLogger.LogWithGuildInfo(guild, "Setting channel property to null for current user");
            await currentUser.ModifyAsync(x => x.Channel = null);
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    private bool RemoveStoredAudioClient(IGuild guild, out IAudioClient? audioClient) =>
        _audioStore.RemoveAudioClientFromGuild(guild, out audioClient);

    private async Task StopAudioClient(IGuild guild, IAudioClient audioClient)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Stopping audio client");
            await audioClient.StopAsync();
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    private void DisposeAudioClient(IGuild guild, IDisposable audioClient)
    {
        try
        {
            _audioLogger.LogWithGuildInfo(guild, "Disposing of audio client");
            audioClient.Dispose();
        }
        catch (Exception e)
        {
            _audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    #endregion
}