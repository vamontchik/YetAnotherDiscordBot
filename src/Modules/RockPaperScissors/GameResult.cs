using System.Threading.Tasks;

namespace DiscordBot.Modules.RockPaperScissors;

public sealed class GameResult
{
    public required RpsPlayer P1 { get; init; }
    public required RpsPlayer P2 { get; init; }
    public required GameResultType WinType { get; init; }

    public Task<RpsPlayer?> GetWinnerAsync()
    {
        var winningPlayer = WinType switch
        {
            GameResultType.P1 => P1,
            GameResultType.P2 => P2,
            _ => null
        };
        return Task.FromResult(winningPlayer);
    }

    public Task<RpsPlayer?> GetLoserAsync()
    {
        var losingPlayer = WinType switch
        {
            GameResultType.P1 => P2,
            GameResultType.P2 => P1,
            _ => null
        };
        return Task.FromResult(losingPlayer);
    }
}