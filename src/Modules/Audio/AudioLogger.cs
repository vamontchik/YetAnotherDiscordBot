using System;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IAudioLogger
{
    void LogWithGuildInfo(IGuild guild, string message);
    void LogExceptionWithGuildInfo(IGuild guild, Exception e);
}

public class AudioLogger : IAudioLogger
{
    public void LogExceptionWithGuildInfo(IGuild guild, Exception e) =>
        Console.WriteLine($"[{guild.Name}:{guild.Id}] {e}");

    public void LogWithGuildInfo(IGuild guild, string message) =>
        Console.WriteLine($"[{guild.Name}:{guild.Id}] {message}");
}