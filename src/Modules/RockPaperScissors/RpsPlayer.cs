namespace DiscordBot.Modules.RockPaperScissors;

public sealed class RpsPlayer
{
    public const ulong BotId = 0;

    public RpsPlayer(RpsType type, string name, ulong id)
    {
        Type = type;
        Name = name;
        Id = id;
    }

    public RpsType Type { get; }
    public string Name { get; }
    public ulong Id { get; }
}