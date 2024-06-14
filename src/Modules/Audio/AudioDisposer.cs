using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace DiscordBot.Modules.Audio;

internal interface IAudioDisposer
{
    Task CleanupFfmpegProcessAsync(IGuild guild);
    Task CleanupFfmpegStreamAsync(IGuild guild);
    Task CleanupPcmStreamAsync(IGuild guild);
    Task CleanupAudioClientAsync(IGuild guild);
}

internal sealed class AudioDisposer(IAudioStore audioStore, IAudioLogger audioLogger) : IAudioDisposer
{
    #region CleanupFfmpegProcessAsync

    public Task CleanupFfmpegProcessAsync(IGuild guild)
    {
        if (!RemoveStoredFfmpegProcess(guild, out var ffmpegProcess) || ffmpegProcess is null)
            return Task.CompletedTask;

        _ = DisposeProcess(guild, ffmpegProcess);
        return Task.CompletedTask;
    }

    private bool RemoveStoredFfmpegProcess(IGuild guild, out Process? ffmpegProcess) =>
        audioStore.RemoveFfmpegProcessFromGuild(guild, out ffmpegProcess);

    private bool DisposeProcess(IGuild guild, IDisposable ffmpegProcess)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Disposing of ffmpeg process");
            ffmpegProcess.Dispose();
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    #endregion

    #region CleanupFfmpegStreamAsync

    public async Task CleanupFfmpegStreamAsync(IGuild guild)
    {
        if (!RemoveStoredFfmpegStream(guild, out var ffmpegStream) || ffmpegStream is null)
            return;

        _ = await DisposeFfmpegStream(guild, ffmpegStream).ConfigureAwait(false);
    }

    private bool RemoveStoredFfmpegStream(IGuild guild, out Stream? ffmpegStream) =>
        audioStore.RemoveFfmpegStreamFromGuild(guild, out ffmpegStream);

    private async Task<bool> DisposeFfmpegStream(IGuild guild, IAsyncDisposable ffmpegStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Disposing of ffmpeg stream");
            await ffmpegStream.DisposeAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    #endregion

    #region CleanupPcmStreamAsync

    public async Task CleanupPcmStreamAsync(IGuild guild)
    {
        if (!RemoveStoredPcmStream(guild, out var pcmStream) || pcmStream is null)
            return;

        _ = await DisposePcmStream(guild, pcmStream).ConfigureAwait(false);    
    }

    private bool RemoveStoredPcmStream(IGuild guild, out AudioOutStream? pcmStream) =>
        audioStore.RemovePcmStreamFromGuild(guild, out pcmStream);

    private async Task<bool> DisposePcmStream(IGuild guild, IAsyncDisposable pcmStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Disposing of pcm stream");
            await pcmStream.DisposeAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    #endregion

    #region CleanupAudioClientAsync

    public async Task CleanupAudioClientAsync(IGuild guild)
    {
        if (!RemoveStoredAudioClient(guild, out var audioClient) || audioClient is null)
            return;

        await StopAudioClient(guild, audioClient).ConfigureAwait(false);
        await EraseChannelPropertyOfCurrentUser(guild).ConfigureAwait(false);
        _ = DisposeAudioClient(guild, audioClient);
    }

    private async Task EraseChannelPropertyOfCurrentUser(IGuild guild)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Obtaining current user");
            var currentUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
            audioLogger.LogWithGuildInfo(guild, "Setting channel property to null for current user");
            await currentUser.ModifyAsync(x => x.Channel = null).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    private bool RemoveStoredAudioClient(IGuild guild, out IAudioClient? audioClient) =>
        audioStore.RemoveAudioClientFromGuild(guild, out audioClient);

    private async Task StopAudioClient(IGuild guild, IAudioClient audioClient)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Stopping audio client");
            await audioClient.StopAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    private bool DisposeAudioClient(IGuild guild, IDisposable audioClient)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Disposing of audio client");
            audioClient.Dispose();
            return true;
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
            return false;
        }
    }

    #endregion
}