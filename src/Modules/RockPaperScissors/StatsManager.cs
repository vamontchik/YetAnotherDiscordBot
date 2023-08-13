using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.Modules.RockPaperScissors;

public sealed class StatsManager
{
    private readonly ConcurrentDictionary<ulong, Stat> _idToInfo = new();

    public async Task UpdateAsync(GameResult gameResult)
    {
        var winner = await gameResult.GetWinnerAsync();
        if (winner is null)
        {
            await UpdateBothForTieAsync(new List<RpsPlayer> { gameResult.P1, gameResult.P2 });
        }
        else
        {
            var loser = await gameResult.GetLoserAsync()
                        ?? throw new InvalidOperationException("Unable to obtain a loser");
            await UpdatePlayerAsync(winner, StatResultType.Win);
            await UpdatePlayerAsync(loser, StatResultType.Loss);
        }
    }

    private async Task UpdateBothForTieAsync(IEnumerable<RpsPlayer> players)
    {
        foreach (var p in players)
            await UpdatePlayerAsync(p, StatResultType.Tie);
    }

    private async Task UpdatePlayerAsync(RpsPlayer p, StatResultType statType)
    {
        var statForPlayer = GetFromDictOrNew(p.Id);
        await statForPlayer.UpdateAsync(p.Type, statType);
    }

    private Stat GetFromDictOrNew(ulong id)
    {
        var success = _idToInfo.TryGetValue(id, out var stat);
        if (success)
            return stat!;

        stat = new Stat();
        _idToInfo[id] = stat;
        return stat;
    }

    public Task<Stat?> GetStatAsync(ulong id)
    {
        var success = _idToInfo.TryGetValue(id, out var stat);
        return Task.FromResult(success ? stat : null);
    }
}