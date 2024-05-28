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

public sealed class AudioDisposer(IAudioStore audioStore, IAudioLogger audioLogger) : IAudioDisposer
{
    #region CleanupFfmpegProcessAsync

    public async Task CleanupFfmpegProcessAsync(IGuild guild)
    {
        if (!RemoveStoredFfmpegProcess(guild, out var ffmpegProcess) || ffmpegProcess is null)
            return;

        if (!DisposeProcess(guild, ffmpegProcess))
            return;

        audioLogger.LogWithGuildInfo(guild, "Removed and disposed of ffmpeg process");
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

        if (!await DisposeFfmpegStream(guild, ffmpegStream))
            return;

        audioLogger.LogWithGuildInfo(guild, "Removed and disposed of ffmpeg stream");
    }

    private bool RemoveStoredFfmpegStream(IGuild guild, out Stream? ffmpegStream) =>
        audioStore.RemoveFfmpegStreamFromGuild(guild, out ffmpegStream);

    private async Task<bool> DisposeFfmpegStream(IGuild guild, IAsyncDisposable ffmpegStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Disposing of ffmpeg stream");
            await ffmpegStream.DisposeAsync();
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

        if (!await DisposePcmStream(guild, pcmStream))
            return;

        audioLogger.LogWithGuildInfo(guild, "Removed and disposed of pcm stream");
    }

    private bool RemoveStoredPcmStream(IGuild guild, out AudioOutStream? pcmStream) =>
        audioStore.RemovePcmStreamFromGuild(guild, out pcmStream);

    private async Task<bool> DisposePcmStream(IGuild guild, IAsyncDisposable pcmStream)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Disposing of pcm stream");
            await pcmStream.DisposeAsync();
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

        await StopAudioClient(guild, audioClient);
        await EraseChannelPropertyOfCurrentUser(guild);
        DisposeAudioClient(guild, audioClient);

        audioLogger.LogWithGuildInfo(guild, "Removed, stopped, and disposed of audio client");
    }

    private async Task EraseChannelPropertyOfCurrentUser(IGuild guild)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Obtaining current user");
            var currentUser = await guild.GetCurrentUserAsync();
            audioLogger.LogWithGuildInfo(guild, "Setting channel property to null for current user");
            await currentUser.ModifyAsync(x => x.Channel = null);
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
            await audioClient.StopAsync();
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    private void DisposeAudioClient(IGuild guild, IDisposable audioClient)
    {
        try
        {
            audioLogger.LogWithGuildInfo(guild, "Disposing of audio client");
            audioClient.Dispose();
        }
        catch (Exception e)
        {
            audioLogger.LogExceptionWithGuildInfo(guild, e);
        }
    }

    #endregion
}