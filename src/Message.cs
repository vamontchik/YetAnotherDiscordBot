using Discord.WebSocket;

namespace DiscordBot
{
    internal class Message
    {
        public Message(SocketMessage msg, bool isAdmin)
        {
            SocketMessage = msg;
            IsAdminMessage = isAdmin;
        }

        public SocketMessage SocketMessage { get; }
        public bool IsAdminMessage { get; }
    }
}