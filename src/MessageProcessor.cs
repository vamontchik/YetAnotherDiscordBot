using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot
{
    internal class MessageProcessor
    {
        private readonly HashSet<string> _commands;

        public MessageProcessor()
        {
            _commands = new HashSet<string>
            {
                "!rps",
                "!stats"
            };
        }

        public async Task ProcessMessage(Message message)
        {
            // TODO: when to implement use of message.IsAdmin ?
            var command = ParseMessage(message.SocketMessage);
            await command.ExecuteAsync();
        }

        private ICommand ParseMessage(SocketMessage socketMessage)
        {
            var contents = socketMessage.Content;
            var split = contents.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return IsValidCommand(split) ? ParseIntoCommand(split, socketMessage) : new EmptyCommand() ;
        }

        private bool IsValidCommand(IReadOnlyList<string> splitMessageContents)
        {
            if (splitMessageContents.Count == 0)
                return false;

            var baseCommand = splitMessageContents[0];
            return _commands.Contains(baseCommand);
        }

        private static ICommand ParseIntoCommand( IReadOnlyList<string> splitMessageContents, SocketMessage socketMessage)
        {
            var baseCommand = splitMessageContents[0];
            return baseCommand switch
            {
                "!rps" => new RockPaperScissorsCommand(splitMessageContents, socketMessage),
                "!stats" => new StatsCommand(socketMessage),
                _ => new EmptyCommand()
            };
        }
    }
}