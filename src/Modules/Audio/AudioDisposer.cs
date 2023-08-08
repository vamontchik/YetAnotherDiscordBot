using System;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

public sealed class AudioDisposer
{
    private readonly AudioStore _audioStore;

    private const string FailureToRemoveErrorMessageBase =
        "Unable to remove {0} from internal storage";

    private const string FailureToDisposeMessageBase =
        "Unable to dispose of {0}";

    public AudioDisposer(AudioStore audioStore)
    {
        _audioStore = audioStore;
    }

    public Task<(bool, string)> SafeCleanupFfmpegProcessAsync(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removing and disposing ffmpeg process");

        var didRemoveSucceed = _audioStore.RemoveFfmpegProcessFromGuild(guildId, out var ffmpegProcess);
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
        
        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removed and disposed of ffmpeg process");

        return Task.FromResult((true, string.Empty));
    }

    public async Task<(bool, string)> SafeCleanupFfmpegStreamAsync(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removing and disposing ffmpeg stream");

        var didRemoveSucceed = _audioStore.RemoveFfmpegStreamFromGuild(guildId, out var ffmpegStream);
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
        
        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removed and disposed of ffmpeg stream");

        return (true, string.Empty);
    }

    public async Task<(bool, string)> SafeCleanupPcmStreamAsync(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removing and disposing pcm stream");

        var didRemoveSucceed = _audioStore.RemovePcmStreamFromGuild(guildId, out var pcmStream);
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
        
        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removed and disposed of pcm stream");

        return (true, string.Empty);
    }

    public async Task<(bool, string)> SafeCleanupAudioClientAsync(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var id = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, id, "Removing, stopping, and disposing audio client");

        var didRemoveSucceed = _audioStore.RemoveAudioClientFromGuild(guildId, out var audioClient);
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
            currentUser = await guildCurrentlyIn.GetCurrentUserAsync();
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
        
        AudioLogger.PrintWithGuildInfo(guildName, id, "Removed, stopped, and disposed of audio client");

        return (true, string.Empty);
    }
}