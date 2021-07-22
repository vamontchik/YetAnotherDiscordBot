using Discord.WebSocket;
using DiscordBot.src;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot
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

            return ParseIntoCommand(split, socketMessage.Channel);
        }

        private bool IsValidCommand(string[] splitMessageContents)
        {
            if (splitMessageContents.Length <= 1)
                return false;

            var baseCommand = splitMessageContents[0];
            return _commandToType.ContainsKey(baseCommand);
        }

        private ICommand ParseIntoCommand(string[] splitMessageContents, ISocketMessageChannel messageChannel)
        {
            var baseCommand = splitMessageContents[0];
            var userChoice = splitMessageContents[1];

            var type = _commandToType[baseCommand];

            // how to use magic to get around types ... :)
            object[] argsToConstructor = new object[] { userChoice, messageChannel };
            return Activator.CreateInstance(type, argsToConstructor) as ICommand;
        }
    }
}