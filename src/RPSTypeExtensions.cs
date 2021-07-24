namespace DiscordBot
{
    internal static class RpsTypeExtensions
    {
        public static int Compare(this RpsType ours, RpsType theirs)
        {
            if (ours == RpsType.Rock && theirs == RpsType.Scissors ||
                ours == RpsType.Paper && theirs == RpsType.Rock ||
                ours == RpsType.Scissors && theirs == RpsType.Paper
            )
                return 1;

            if (theirs == RpsType.Rock && ours == RpsType.Scissors ||
                theirs == RpsType.Paper && ours == RpsType.Rock ||
                theirs == RpsType.Scissors && ours == RpsType.Paper
            )
                return -1;

            return 0;
        }
    }
}
