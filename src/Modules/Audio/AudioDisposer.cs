using System;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

public sealed class AudioDisposer
{
    private readonly AudioStore _audioStore;

    public AudioDisposer(AudioStore audioStore)
    {
        _audioStore = audioStore;
    }

    public Task<bool> CleanupFfmpegProcessAsync(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removing and disposing ffmpeg process");

        var didRemoveSucceed = _audioStore.RemoveFfmpegProcessFromGuild(guildId, out var ffmpegProcess);
        if (!didRemoveSucceed)
            return Task.FromResult(false);

        try
        {
            ffmpegProcess?.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task<bool> CleanupFfmpegStreamAsync(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removing and disposing ffmpeg stream");

        var didRemoveSucceed = _audioStore.RemoveFfmpegStreamFromGuild(guildId, out var ffmpegStream);
        if (!didRemoveSucceed)
            return false;

        try
        {
            await (ffmpegStream?.DisposeAsync() ?? ValueTask.CompletedTask);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<bool> CleanupPcmStream(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var guildIdStr = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, guildIdStr, "Removing and disposing pcm stream");

        var didRemoveSucceed = _audioStore.RemovePcmStreamFromGuild(guildId, out var pcmStream);
        if (!didRemoveSucceed)
            return false;

        try
        {
            await (pcmStream?.DisposeAsync() ?? ValueTask.CompletedTask);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<bool> CleanupAudioClient(IGuild guildCurrentlyIn)
    {
        var guildName = guildCurrentlyIn.Name;
        var guildId = guildCurrentlyIn.Id;
        var id = guildId.ToString();

        AudioLogger.PrintWithGuildInfo(guildName, id, "Removing, stopping, and disposing audio client");

        var didRemoveSucceed = _audioStore.RemoveAudioClientFromGuild(guildId, out var audioClient);
        if (!didRemoveSucceed)
            return false;

        try
        {
            await (audioClient?.StopAsync() ?? Task.CompletedTask);

            var currentUser = await guildCurrentlyIn.GetCurrentUserAsync();
            await currentUser.ModifyAsync(x => x.Channel = null);

            audioClient?.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }
}