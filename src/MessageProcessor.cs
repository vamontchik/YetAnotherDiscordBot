using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBot.Command;
using DiscordBot.Command.Empty;
using DiscordBot.Command.Gremlin;
using DiscordBot.Command.RockPaperScissors;
using DiscordBot.Command.Stats;

namespace DiscordBot
{
    internal class MessageProcessor
    {
        #region Singleton

        private MessageProcessor()
        {
            _commands = new HashSet<string>
            {
                "!rps",
                "!stats",
                "!gremlin"
            };
        }
        
        private static readonly Lazy<MessageProcessor> Lazy = new(() => new MessageProcessor());

        public static MessageProcessor Instance => Lazy.Value;

        #endregion

        private readonly HashSet<string> _commands;

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
            return IsValidCommand(split) ? ParseIntoCommand(split, socketMessage) : new EmptyCommand();
        }

        private bool IsValidCommand(IReadOnlyList<string> splitMessageContents)
        {
            if (splitMessageContents.Count == 0)
                return false;

            var baseCommand = splitMessageContents[0];
            return _commands.Contains(baseCommand);
        }

        private static ICommand ParseIntoCommand(
            IReadOnlyList<string> splitMessageContents, SocketMessage socketMessage)
        {
            var baseCommand = splitMessageContents[0];
            return baseCommand switch
            {
                "!rps" => new RockPaperScissorsCommand(splitMessageContents, socketMessage),
                "!stats" => new StatsCommand(socketMessage),
                "!gremlin" => new GremlinCommand(socketMessage),
                _ => new EmptyCommand()
            };
        }
    }
}