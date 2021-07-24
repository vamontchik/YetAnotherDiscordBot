namespace DiscordBot.src
{
    class GameResult
    {
        private readonly RPSPlayer _p1;
        private readonly RPSPlayer _p2;
        private readonly GameResultType _winType;

        public GameResult(RPSPlayer p1, RPSPlayer p2, GameResultType winType)
        {
            _p1 = p1;
            _p2 = p2;
            _winType = winType;
        }

        public RPSPlayer? GetWinner()
        {
            if (_winType == GameResultType.P1)
                return _p1;
            else if (_winType == GameResultType.P2)
                return _p2;
            else // _winType == GameResult.Tie
                return null;
        }

        public RPSPlayer? GetLoser()
        {
            if (_winType == GameResultType.P1)
                return _p2;
            else if (_winType == GameResultType.P2)
                return _p1;
            else // _winType == GameResult.Tie
                return null;
        }
        public RPSPlayer P1 => _p1;
        public RPSPlayer P2 => _p2;
    }
}