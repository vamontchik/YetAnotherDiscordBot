namespace DiscordBot.Modules.RockPaperScissors;

internal sealed class RpsPlayer
{
    public const ulong BotId = 0;

    public required ulong Id { get; init; }
    public required string Name { get; init; }
    public required RpsType Type { get; init; }
}