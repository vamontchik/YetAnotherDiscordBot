using Discord.WebSocket;
using DiscordBot.src;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.src
{
    class MessageProcessor
    {
        private readonly Dictionary<string, Type> _commandToType;

        public MessageProcessor()
        {
            _commandToType = new Dictionary<string, Type>
            {
                { "!rps", typeof(RockPaperScissorsCommand) }
            };
        }

        public async Task ProcessMessage(Message message)
        {
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
            return _commandToType.ContainsKey(baseCommand);
        }

        private ICommand ParseIntoCommand(string[] splitMessageContents, SocketMessage socketMessage)
        {
            var baseCommand = splitMessageContents[0];

            string userChoice = GetUserChoiceOrDefault(splitMessageContents, "");

            var type = _commandToType[baseCommand];

            // !!!
            object[] argsToConstructor = new object[] { userChoice, socketMessage };
            return Activator.CreateInstance(type, argsToConstructor) as ICommand;
        }

        private static string GetUserChoiceOrDefault(string[] splitMessageContents, string defaultStr)
        {
            if (splitMessageContents.Length == 1)
                return defaultStr;
            else
                return splitMessageContents[1].ToLower();
        }
    }
}