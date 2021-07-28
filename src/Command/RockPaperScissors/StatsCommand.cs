using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot.Command.RockPaperScissors
{
    internal class StatsEntryPoint
    {
        private readonly ISocketMessageChannel _destChannel;
        private readonly ulong _userId;
        private readonly string _userName;

        public StatsEntryPoint(SocketMessage socketMessage)
        {
            _destChannel = socketMessage.Channel;
            _userId = socketMessage.Author.Id;
            _userName = socketMessage.Author.Username;
        }

        public async Task DoAsync()
        {
            var statsManager = StatsManager.Instance;
            var stat = statsManager.GetStat(_userId);
            if (stat is null)
                await _destChannel.SendMessageAsync($"No stats found for {_userName}");
            else
            {
                var statsStr = stat.ComputeStats();
                await _destChannel.SendMessageAsync($"{statsStr}");
            }
        }
    }
}
