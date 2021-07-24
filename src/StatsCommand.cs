using Discord.WebSocket;
using System.Threading.Tasks;

namespace DiscordBot.src
{
    class StatsCommand : ICommand
    {
        private readonly ISocketMessageChannel _destChannel;
        private readonly ulong _userID;
        private readonly string _userName;

        public StatsCommand(SocketMessage socketMessage)
        {
            _destChannel = socketMessage.Channel;
            _userID = socketMessage.Author.Id;
            _userName = socketMessage.Author.Username;
        }

        public async Task ExecuteAsync()
        {
            var statsManager = StatsManager.Instance;
            var stat = statsManager.GetStat(_userID);
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
