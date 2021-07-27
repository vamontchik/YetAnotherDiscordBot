using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot.Command.Gremlin
{
    internal class GremlinCommand : ICommand
    {
        private readonly ISocketMessageChannel _destChannel;

        public GremlinCommand(SocketMessage socketMessage)
        {
            _destChannel = socketMessage.Channel;
        }

        public async Task ExecuteAsync()
        {
            var gremlin = Gremlin.Instance;
            gremlin.Update();
            await _destChannel.SendMessageAsync($"Gremlin updated! New stats: {gremlin}");
        }
    }
}