using System;

namespace DiscordBot
{
    class MessageProcessor
    {
        public void ProcessMessage(Message message)
        {
            var socketMessage = message.SocketMessage;
            var adminID = message.AdminID;
            var userID = socketMessage.Author.Id;
            if (userID.ToString() == adminID)
            {
                Console.WriteLine($"Admin: {socketMessage.Content}");
            }
            else
            {
                Console.WriteLine($"Non-admin: {socketMessage.Content}");
            }
        }
    }
}