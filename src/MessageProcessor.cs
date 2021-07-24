using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.src
{
    class MessageProcessor
    {
        private readonly HashSet<string> _commands;

        public MessageProcessor()
        {
            _commands = new()
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

            if (!IsValidCommand(split))
                return new EmptyCommand();

            return ParseIntoCommand(split, socketMessage);
        }

        private bool IsValidCommand(string[] splitMessageContents)
        {
            if (splitMessageContents.Length == 0)
                return false;

            var baseCommand = splitMessageContents[0];
            return _commands.Contains(baseCommand);
        }

        private static ICommand ParseIntoCommand(string[] splitMessageContents, SocketMessage socketMessage)
        {
            var baseCommand = splitMessageContents[0];
            return baseCommand switch
            {
                "!rps" => new RockPaperScissorsCommand(splitMessageContents, socketMessage),
                "!stats" => new StatsCommand(socketMessage),
                _ => new EmptyCommand(),
            };
        }
    }
}