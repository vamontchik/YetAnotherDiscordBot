using System;

namespace DiscordBot.Modules.Audio;

public static class AudioLogger
{
    public static void PrintWithGuildInfo(string guildName, string guildId, string message) =>
        Console.WriteLine($"[{guildName}:{guildId}] {message}");
}