using Discord.WebSocket;

namespace DiscordBot.src
{
    class Message
    {
        private readonly SocketMessage _socketMessage;
        private readonly bool _isAdmin;

        public Message(SocketMessage msg, bool isAdmin)
        {
            _socketMessage = msg;
            _isAdmin = isAdmin;
        }

        public SocketMessage SocketMessage { get => _socketMessage; }
        public bool IsAdminMessage { get => _isAdmin; }
    }
}