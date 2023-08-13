using System;
using Discord;

namespace DiscordBot.Modules.Audio;

public interface IAudioLogger
{
    void LogWithGuildInfo(IGuild guild, string message);
}

public class AudioLogger : IAudioLogger
{
    public void LogWithGuildInfo(IGuild guild, string message) =>
        Console.WriteLine($"[{guild.Name}:{guild.Id}] {message}");
}