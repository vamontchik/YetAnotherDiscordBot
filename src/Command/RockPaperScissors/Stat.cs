using System;
using System.Collections.Generic;

namespace DiscordBot.Command.RockPaperScissors
{
    internal class Stat
    {
        private readonly Dictionary<RpsType, int> _winsByType;
        private readonly Dictionary<RpsType, int> _totalByType;

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
            _totalByType[rpsType] += 1;
            
            if (statType == StatResultType.Win)
            {
                _winsByType[rpsType] += 1;
            }
        }

        public string ComputeStats()
        {
            var res = "";
            var types = Enum.GetValues(typeof(RpsType));

            foreach (RpsType t in types)
            {
                var winsForType = _winsByType[t];
                var totalForType = _totalByType[t];
                var percentage = CalculatePercent(winsForType, totalForType);
                res += $"{t} : {winsForType} wins, {totalForType} games, {percentage}% win rate";
                res += Environment.NewLine;
            }
            
            return res;
        }

        private static int CalculatePercent(int wins, int total)
        {
            return total == 0 ? 0 : Convert.ToInt32(100 * (1.0 * wins / total));
        }
    }
}