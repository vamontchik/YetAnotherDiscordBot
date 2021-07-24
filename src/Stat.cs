using System;
using System.Collections.Generic;

namespace DiscordBot.src
{
    class Stat
    {
        private Dictionary<RPSType, int> _winsByType;
        private Dictionary<RPSType, int> _totalByType;

        public Stat()
        {
            _winsByType = new()
            {
                { RPSType.Rock, 0 },
                { RPSType.Paper, 0 },
                { RPSType.Scissors, 0 }
            };

            _totalByType = new()
            {
                { RPSType.Rock, 0 },
                { RPSType.Paper, 0 },
                { RPSType.Scissors, 0 }
            };
        }

        public void Update(RPSType rpsType, StatResultType statType)
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
            var types = Enum.GetValues(typeof(RPSType));

            foreach (RPSType t in types)
            {
                var winsForType = _winsByType[t];
                var totalForType = _totalByType[t];
                var percentage = CalculatePercent(wins: winsForType, total: totalForType);
                res += $"{t} : {winsForType} wins, {totalForType} games, {percentage}% winrate";
                res += Environment.NewLine;
            }
            
            return res;
        }

        private int CalculatePercent(int wins, int total)
        {
            return total == 0 ? 0 : Convert.ToInt32(100 * (1.0 * wins / total));
        }
    }
}