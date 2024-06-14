using System;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IAudioLogger
{
    void LogWithGuildInfo(IGuild guild, string message);
    void LogExceptionWithGuildInfo(IGuild guild, Exception e);
}

public sealed class AudioLogger : IAudioLogger
{
    public void LogExceptionWithGuildInfo(IGuild guild, Exception e) =>
        Console.WriteLine($"[{DateTime.Now}][{guild.Name}:{guild.Id}] {e}");

    public void LogWithGuildInfo(IGuild guild, string message) =>
        Console.WriteLine($"[{DateTime.Now}][{guild.Name}:{guild.Id}] {message}");
}