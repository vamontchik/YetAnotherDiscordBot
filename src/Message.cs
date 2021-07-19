using Discord.WebSocket;

namespace DiscordBot
{
    class Message
    {
        public SocketMessage SocketMessage { get; set; }
        public string AdminID { get; set; }
    }
}