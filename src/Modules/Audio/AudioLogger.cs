using System;
using Discord;

namespace DiscordBot.Modules.Audio;

internal interface IAudioLogger
{
    void LogWithGuildInfo(IGuild guild, string message);
    void LogExceptionWithGuildInfo(IGuild guild, Exception e);
}

internal sealed class AudioLogger : IAudioLogger
{
    public void LogExceptionWithGuildInfo(IGuild guild, Exception e) =>
        Console.WriteLine($"[{DateTime.Now}][{guild.Name}:{guild.Id}] {e}");

    public void LogWithGuildInfo(IGuild guild, string message) =>
        Console.WriteLine($"[{DateTime.Now}][{guild.Name}:{guild.Id}] {message}");
}