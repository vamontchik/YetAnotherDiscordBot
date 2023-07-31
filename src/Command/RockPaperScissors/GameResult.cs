namespace DiscordBot.Command.RockPaperScissors;

internal class GameResult
{
    private readonly GameResultType _winType;

    public GameResult(RpsPlayer p1, RpsPlayer p2, GameResultType winType)
    {
        P1 = p1;
        P2 = p2;
        _winType = winType;
    }

    public RpsPlayer? GetWinner() => _winType switch
    {
        GameResultType.P1 => P1,
        GameResultType.P2 => P2,
        _ => null
    };

    public RpsPlayer? GetLoser() => _winType switch
    {
        GameResultType.P1 => P2,
        GameResultType.P2 => P1,
        _ => null
    };

    public RpsPlayer P1 { get; }

    public RpsPlayer P2 { get; }
}