using Discord.WebSocket;

namespace DiscordBot
{
    class Message
    {
        private SocketMessage _socketMessage;
        private bool _isAdmin;

        public Message(SocketMessage msg, bool isAdmin)
        {
            _socketMessage = msg;
            _isAdmin = isAdmin;
        }

        public SocketMessage SocketMessage { get => _socketMessage; }
        public bool IsAdminMessage { get => _isAdmin; }
    }
}