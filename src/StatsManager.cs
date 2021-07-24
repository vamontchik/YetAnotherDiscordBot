using System;
using System.Collections.Generic;

namespace DiscordBot.src
{
    class StatsManager
    {
        private StatsManager()
        {
            _idToInfo = new();
        }
        
        private static readonly Lazy<StatsManager> _lazy = new(() => new StatsManager());

        public static StatsManager Instance => _lazy.Value;

        //////////////
        //////////////
        //////////////

        private Dictionary<ulong, Stat> _idToInfo;

        public void Update(GameResult gameResult)
        {
            var nullableWinner = gameResult.GetWinner();
            if (nullableWinner is null)
                UpdateBothForTie(new List<RPSPlayer>(){ gameResult.P1, gameResult.P2 });
            else
            {
                RPSPlayer winner = nullableWinner; // NOTE: can't be null, so "cast" to a non-nullable object
                RPSPlayer loser = gameResult.GetLoser(); // NOTE: can't be null if we have a non-null winner !
                
                UpdatePlayer(winner, StatResultType.Win);
                UpdatePlayer(loser, StatResultType.Loss);
            }
        }

        private void UpdateBothForTie(List<RPSPlayer> players)
        {
            foreach (RPSPlayer p in players)
            {
                var statForPlayer = GetFromDictOrNew(p.Id);
                statForPlayer.Update(p.Type, StatResultType.Tie);
            }
        }

        private void UpdatePlayer(RPSPlayer p, StatResultType statType)
        {
            var statForPlayer = GetFromDictOrNew(p.Id);
            statForPlayer.Update(p.Type, statType);
        }

        private Stat GetFromDictOrNew(ulong id)
        {
            Stat stat;
            var success = _idToInfo.TryGetValue(id, out stat);
            if (success)
                return stat;

            stat = new Stat();
            _idToInfo.Add(id, stat);
            return stat;
        }

        public Stat? GetStat(ulong id)
        {
            Stat stat;
            var success = _idToInfo.TryGetValue(id, out stat);
            if (success)
                return stat;
            return null;
        }
    }
}