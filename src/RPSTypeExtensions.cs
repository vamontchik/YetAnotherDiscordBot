namespace DiscordBot.src
{
    static class RPSTypeExtensions
    {
        public static int Compare(this RPSType ours, RPSType theirs)
        {
            if (ours == RPSType.Rock && theirs == RPSType.Scissors ||
                ours == RPSType.Paper && theirs == RPSType.Rock ||
                ours == RPSType.Scissors && theirs == RPSType.Paper
            )
                return 1;

            if (theirs == RPSType.Rock && ours == RPSType.Scissors ||
                theirs == RPSType.Paper && ours == RPSType.Rock ||
                theirs == RPSType.Scissors && ours == RPSType.Paper
            )
                return -1;

            return 0;
        }
    }
}
