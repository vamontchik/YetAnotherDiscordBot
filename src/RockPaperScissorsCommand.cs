using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.src
{
    class RockPaperScissorsCommand : ICommand
    {
        private readonly Random _rnd;
        private readonly Dictionary<string, RPSType> _strChoices;
        private readonly Dictionary<int, RPSType> _choices;
        private readonly string _userChoice;
        private readonly ISocketMessageChannel _destChannel;
        private readonly SocketUser _user;

        public RockPaperScissorsCommand(string userChoice, SocketMessage socketMessage)
        {
            _rnd = CreateRandom();
            _choices = CreateChoicesDictionary();
            _strChoices = CreateStrChoicesDictionary();
            _userChoice = userChoice;
            _destChannel = socketMessage.Channel;
            _user = socketMessage.Author;
        }

        private static Random CreateRandom()
        {
            return new Random();
        }

        private static Dictionary<int, RPSType> CreateChoicesDictionary()
        {
            return new()
            {
                { 0, RPSType.Rock },
                { 1, RPSType.Paper },
                { 2, RPSType.Scissors }
            };
        }

        private static Dictionary<string, RPSType> CreateStrChoicesDictionary()
        {
            return new()
            {
                { "rock", RPSType.Rock },
                { "paper", RPSType.Paper },
                { "scissors", RPSType.Scissors }
            };
        }

        public async Task ExecuteAsync()
        {
            if (!ValidUserChoice())
                await SendErrorMessageAsync();
            else
                await SendGameMessageAsync();
        }

        private bool ValidUserChoice()
        {
            return _strChoices.ContainsKey(_userChoice.ToLower());
        }

        private async Task SendErrorMessageAsync()
        {
            await _destChannel.SendMessageAsync("That's not a rock, paper, or scissors!");
        }

        private async Task SendGameMessageAsync()
        {
            var bot = CreateBotPlayer();
            var user = CreateUserPlayer();
            var winner = GetWinner(bot, user);
            await _destChannel.SendMessageAsync($"user: {user.Type}, Bot: {bot.Type}, Winner: {winner}");
        }

        private RPSPlayer CreateBotPlayer()
        {
            int botChoice = CreateRandomBotChoice();
            var botChoiceAsType = _choices[botChoice];
            return new RPSPlayer(botChoiceAsType, "Bot");
        }

        private int CreateRandomBotChoice()
        {
            return _rnd.Next(Enum.GetNames(typeof(RPSType)).Length);
        }

        private RPSPlayer CreateUserPlayer()
        {
            var userChoiceAsType = _strChoices[_userChoice];
            return new RPSPlayer(userChoiceAsType, _user.Username);
        }

        private static string GetWinner(RPSPlayer first, RPSPlayer second)
        {
            var compare = first.Type.Compare(second.Type);

            if (compare == -1)
                return second.Name;
            else if (compare == 1)
                return first.Name;
            else // 0
                return "Tie!";
        }
    }
}
