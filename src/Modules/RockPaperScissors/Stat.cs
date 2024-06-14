using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules.RockPaperScissors;

internal sealed class Stat
{
    private readonly Dictionary<RpsType, int> _winsByType = new()
    {
        { RpsType.Rock, 0 },
        { RpsType.Paper, 0 },
        { RpsType.Scissors, 0 }
    };

    private readonly Dictionary<RpsType, int> _totalByType = new()
    {
        { RpsType.Rock, 0 },
        { RpsType.Paper, 0 },
        { RpsType.Scissors, 0 }
    };

    private static readonly object ReadWriteLock = new();

    public Task UpdateAsync(RpsType rpsType, StatResultType statType)
    {
        lock (ReadWriteLock)
        {
            _totalByType[rpsType] += 1;
            if (statType == StatResultType.Win)
                _winsByType[rpsType] += 1;
        }

        return Task.CompletedTask;
    }

    public Task<string> ComputeStatsAsync()
    {
        lock (ReadWriteLock)
        {
            var result = new StringBuilder();
            var types = Enum.GetValues(typeof(RpsType));

            foreach (RpsType t in types)
            {
                var winsForType = _winsByType[t];
                var totalForType = _totalByType[t];
                var percentage = CalculatePercent(winsForType, totalForType);
                result.Append($"{t} : {winsForType} wins, {totalForType} games, {percentage}% win rate");
                result.Append(Environment.NewLine);
            }

            return Task.FromResult(result.ToString());
        }
    }

    private static int CalculatePercent(int wins, int total) =>
        total == 0 ? 0 : Convert.ToInt32(100 * (1.0 * wins / total));
}