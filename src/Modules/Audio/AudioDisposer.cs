using System;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IAudioDisposer
{
    Task<(bool, string)> CleanupFfmpegProcessAsync(IGuild guild);
    Task<(bool, string)> CleanupFfmpegStreamAsync(IGuild guild);
    Task<(bool, string)> CleanupPcmStreamAsync(IGuild guild);
    Task<(bool, string)> CleanupAudioClientAsync(IGuild guild);
}

public sealed class AudioDisposer : IAudioDisposer
{
    private readonly IAudioStore _audioStore;
    private readonly IAudioLogger _audioLogger;

    private const string FailureToRemoveErrorMessageBase =
        "Unable to remove {0} from internal storage";

    private const string FailureToDisposeMessageBase =
        "Unable to dispose of {0}";

    public AudioDisposer(
        IAudioStore audioStore,
        IAudioLogger audioLogger)
    {
        _audioStore = audioStore;
        _audioLogger = audioLogger;
    }

    public Task<(bool, string)> CleanupFfmpegProcessAsync(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, "Removing and disposing ffmpeg process");

        var didRemoveSucceed = _audioStore.RemoveFfmpegProcessFromGuild(guild.Id, out var ffmpegProcess);
        if (!didRemoveSucceed)
        {
            var errorMessage = string.Format(FailureToRemoveErrorMessageBase, "ffmpeg process");
            return Task.FromResult((false, errorMessage));
        }

        try
        {
            ffmpegProcess?.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var errorMessage = string.Format(FailureToDisposeMessageBase, "ffmpeg process");
            return Task.FromResult((false, errorMessage));
        }

        _audioLogger.LogWithGuildInfo(guild, "Removed and disposed of ffmpeg process");

        return Task.FromResult((true, string.Empty));
    }

    public async Task<(bool, string)> CleanupFfmpegStreamAsync(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, "Removing and disposing ffmpeg stream");

        var didRemoveSucceed = _audioStore.RemoveFfmpegStreamFromGuild(guild.Id, out var ffmpegStream);
        if (!didRemoveSucceed)
        {
            var errorMessage = string.Format(FailureToRemoveErrorMessageBase, "ffmpeg stream");
            return (false, errorMessage);
        }

        try
        {
            await (ffmpegStream?.DisposeAsync() ?? ValueTask.CompletedTask);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var errorMessage = string.Format(FailureToDisposeMessageBase, "ffmpeg stream");
            return (false, errorMessage);
        }

        _audioLogger.LogWithGuildInfo(guild, "Removed and disposed of ffmpeg stream");

        return (true, string.Empty);
    }

    public async Task<(bool, string)> CleanupPcmStreamAsync(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, "Removing and disposing pcm stream");

        var didRemoveSucceed = _audioStore.RemovePcmStreamFromGuild(guild.Id, out var pcmStream);
        if (!didRemoveSucceed)
        {
            var errorMessage = string.Format(FailureToRemoveErrorMessageBase, "pcm stream");
            return (false, errorMessage);
        }

        try
        {
            await (pcmStream?.DisposeAsync() ?? ValueTask.CompletedTask);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var errorMessage = string.Format(FailureToDisposeMessageBase, "pcm stream");
            return (false, errorMessage);
        }

        _audioLogger.LogWithGuildInfo(guild, "Removed and disposed of pcm stream");

        return (true, string.Empty);
    }

    public async Task<(bool, string)> CleanupAudioClientAsync(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(guild, "Removing, stopping, and disposing audio client");

        var didRemoveSucceed = _audioStore.RemoveAudioClientFromGuild(guild.Id, out var audioClient);
        if (!didRemoveSucceed)
        {
            var errorMessage = string.Format(FailureToRemoveErrorMessageBase, "audio client");
            return (false, errorMessage);
        }

        try
        {
            await (audioClient?.StopAsync() ?? Task.CompletedTask);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to stop client");
        }

        IGuildUser currentUser;
        try
        {
            currentUser = await guild.GetCurrentUserAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to retrieve current user");
        }

        try
        {
            await currentUser.ModifyAsync(x => x.Channel = null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (false, "Unable to modify channel value of current user");
        }

        try
        {
            audioClient?.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var errorMessage = string.Format(FailureToDisposeMessageBase, "audio client");
            return (false, errorMessage);
        }

        _audioLogger.LogWithGuildInfo(guild, "Removed, stopped, and disposed of audio client");

        return (true, string.Empty);
    }
}