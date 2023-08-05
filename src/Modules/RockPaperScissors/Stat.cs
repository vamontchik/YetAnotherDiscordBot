using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Modules.RockPaperScissors;

public sealed class Stat
{
    private readonly Dictionary<RpsType, int> _winsByType;
    private readonly Dictionary<RpsType, int> _totalByType;

    private static readonly object ReadWriteLock = new();

    public Stat()
    {
        _winsByType = new Dictionary<RpsType, int>
        {
            { RpsType.Rock, 0 },
            { RpsType.Paper, 0 },
            { RpsType.Scissors, 0 }
        };

        _totalByType = new Dictionary<RpsType, int>
        {
            { RpsType.Rock, 0 },
            { RpsType.Paper, 0 },
            { RpsType.Scissors, 0 }
        };
    }

    public void Update(RpsType rpsType, StatResultType statType)
    {
        lock (ReadWriteLock)
        {
            _totalByType[rpsType] += 1;
            if (statType == StatResultType.Win)
                _winsByType[rpsType] += 1;
        }
    }

    public string ComputeStats()
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

            return result.ToString();
        }
    }

    private static int CalculatePercent(int wins, int total) =>
        total == 0 ? 0 : Convert.ToInt32(100 * (1.0 * wins / total));
}