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

public class AudioCleanupOrganizer : IAudioCleanupOrganizer
{
    private readonly IAudioStore _audioStore;
    private readonly IMusicFileHandler _musicFileHandler;
    private readonly IAudioDisposer _audioDisposer;
    private readonly IAudioLogger _audioLogger;

    public AudioCleanupOrganizer(
        IAudioConnector audioConnector,
        IMusicFileHandler musicFileHandler,
        IAudioDisposer audioDisposer,
        IAudioLogger audioLogger, IAudioStore audioStore)
    {
        _musicFileHandler = musicFileHandler;
        _audioDisposer = audioDisposer;
        _audioLogger = audioLogger;
        _audioStore = audioStore;
    }

    public async Task FullDisconnectAndCleanup(IGuild guild)
    {
        _audioLogger.LogWithGuildInfo(
            guild, 
            $"Checking if bot is connected to a channel in guild {guild.Name}");
        if (_audioStore.GetAudioClientForGuild(guild) is null)
        {
            _audioLogger.LogWithGuildInfo(guild, $"Bot is not in a channel for guild {guild.Name}");
            return;
        }

        var pcmStreamTask = _audioDisposer.CleanupPcmStreamAsync(guild);
        var ffmpegStreamTask = _audioDisposer.CleanupFfmpegStreamAsync(guild);
        var ffmpegProcessTask = _audioDisposer.CleanupFfmpegProcessAsync(guild);
        var musicFileTask = _musicFileHandler.DeleteMusicAsync(guild);
        var audioClientTask = _audioDisposer.CleanupAudioClientAsync(guild);
        await Task.WhenAll(pcmStreamTask, ffmpegStreamTask, ffmpegProcessTask, musicFileTask, audioClientTask);

        var message = 
            $"Disconnected from voice on {guild.Name}, " +
            $"disposed of all stream(s)/process(es)/client(s), " +
            $"and deleted music file";
        _audioLogger.LogWithGuildInfo(guild, message);
    }

    public async Task MusicDownloadFailureCleanup(IGuild guild) => await _musicFileHandler.DeleteMusicAsync(guild);

    public async Task FfmpegSetupFailureCleanup(IGuild guild)
    {
        var streamTask = _audioDisposer.CleanupFfmpegStreamAsync(guild);
        var processTask = _audioDisposer.CleanupFfmpegProcessAsync(guild);
        var musicFileTask = _musicFileHandler.DeleteMusicAsync(guild);
        await Task.WhenAll(streamTask, processTask, musicFileTask);
    }

    public async Task PcmStreamSetupFailureCleanup(IGuild guild)
    {
        var pcmStreamTask = _audioDisposer.CleanupPcmStreamAsync(guild);
        var ffmpegStreamTask = _audioDisposer.CleanupFfmpegStreamAsync(guild);
        var ffmpegProcessTask = _audioDisposer.CleanupFfmpegProcessAsync(guild);
        var musicFileTask = _musicFileHandler.DeleteMusicAsync(guild);
        await Task.WhenAll(pcmStreamTask, ffmpegStreamTask, ffmpegProcessTask, musicFileTask);
    }

    public async Task PostSongCleanup(IGuild guild) => await PcmStreamSetupFailureCleanup(guild);
}