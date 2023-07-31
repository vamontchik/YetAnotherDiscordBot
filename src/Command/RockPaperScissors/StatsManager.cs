using System;
using System.Collections.Generic;

namespace DiscordBot.Command.RockPaperScissors;

internal sealed class StatsManager
{
    private StatsManager()
    {
        _idToInfo = new Dictionary<ulong, Stat>();
    }

    private static readonly Lazy<StatsManager> Lazy = new(() => new StatsManager());

    public static StatsManager Instance => Lazy.Value;

    private readonly Dictionary<ulong, Stat> _idToInfo;

    public void Update(GameResult gameResult)
    {
        var winner = gameResult.GetWinner();
        if (winner is null)
        {
            UpdateBothForTie(new List<RpsPlayer> { gameResult.P1, gameResult.P2 });
        }
        else
        {
            var loser = gameResult.GetLoser()
                        ?? throw new InvalidOperationException("Unable to obtain a loser");
            UpdatePlayer(winner, StatResultType.Win);
            UpdatePlayer(loser, StatResultType.Loss);
        }
    }

    private void UpdateBothForTie(IEnumerable<RpsPlayer> players)
    {
        foreach (var p in players)
        {
            var statForPlayer = GetFromDictOrNew(p.Id);
            statForPlayer.Update(p.Type, StatResultType.Tie);
        }
    }

    private void UpdatePlayer(RpsPlayer p, StatResultType statType)
    {
        var statForPlayer = GetFromDictOrNew(p.Id);
        statForPlayer.Update(p.Type, statType);
    }

    private Stat GetFromDictOrNew(ulong id)
    {
        var success = _idToInfo.TryGetValue(id, out var stat);
        if (success)
            return stat!;

        stat = new Stat();
        _idToInfo.Add(id, stat);
        return stat;
    }

    public Stat? GetStat(ulong id)
    {
        var success = _idToInfo.TryGetValue(id, out var stat);
        return success ? stat : null;
    }
}