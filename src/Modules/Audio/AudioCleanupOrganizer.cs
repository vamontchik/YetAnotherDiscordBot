using System.Threading.Tasks;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IAudioCleanupOrganizer
{
    public Task FullDisconnectAndCleanup(IGuild guild);
    public Task MusicDownloadFailureCleanup(IGuild guild);
    public Task FfmpegSetupFailureCleanup(IGuild guild);
    public Task PcmStreamSetupFailureCleanup(IGuild guild);
    public Task PostSongCleanup(IGuild guild);
}

public sealed class AudioCleanupOrganizer(
    IMusicFileHandler musicFileHandler,
    IAudioDisposer audioDisposer,
    IAudioLogger audioLogger,
    IAudioStore audioStore)
    : IAudioCleanupOrganizer
{
    public async Task FullDisconnectAndCleanup(IGuild guild)
    {
        audioLogger.LogWithGuildInfo(guild, "Checking if bot is in a channel");
        if (audioStore.GetAudioClientForGuild(guild) is null)
        {
            audioLogger.LogWithGuildInfo(guild, "Bot is not in a channel");
            return;
        }

        var pcmStreamTask = audioDisposer.CleanupPcmStreamAsync(guild);
        var ffmpegStreamTask = audioDisposer.CleanupFfmpegStreamAsync(guild);
        var ffmpegProcessTask = audioDisposer.CleanupFfmpegProcessAsync(guild);
        var musicFileTask = musicFileHandler.DeleteMusicAsync(guild);
        var audioClientTask = audioDisposer.CleanupAudioClientAsync(guild);
        await Task
            .WhenAll(pcmStreamTask, ffmpegStreamTask, ffmpegProcessTask, musicFileTask, audioClientTask)
            .ConfigureAwait(false);
    }

    public async Task MusicDownloadFailureCleanup(IGuild guild) => 
        await musicFileHandler.DeleteMusicAsync(guild).ConfigureAwait(false);

    public async Task FfmpegSetupFailureCleanup(IGuild guild)
    {
        var streamTask = audioDisposer.CleanupFfmpegStreamAsync(guild);
        var processTask = audioDisposer.CleanupFfmpegProcessAsync(guild);
        var musicFileTask = musicFileHandler.DeleteMusicAsync(guild);
        await Task.WhenAll(streamTask, processTask, musicFileTask).ConfigureAwait(false);
    }

    public async Task PcmStreamSetupFailureCleanup(IGuild guild)
    {
        var pcmStreamTask = audioDisposer.CleanupPcmStreamAsync(guild);
        var ffmpegStreamTask = audioDisposer.CleanupFfmpegStreamAsync(guild);
        var ffmpegProcessTask = audioDisposer.CleanupFfmpegProcessAsync(guild);
        var musicFileTask = musicFileHandler.DeleteMusicAsync(guild);
        await Task.WhenAll(pcmStreamTask, ffmpegStreamTask, ffmpegProcessTask, musicFileTask).ConfigureAwait(false);
    }

    public async Task PostSongCleanup(IGuild guild) => await PcmStreamSetupFailureCleanup(guild).ConfigureAwait(false);
}