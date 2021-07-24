using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.src
{
    class RockPaperScissorsCommand : ICommand
    {
        private readonly Random _rnd;
        private readonly Dictionary<int, RPSType> _choices;
        private readonly Dictionary<string, RPSType> _strChoices;
        private readonly string _userChoice;
        private readonly ISocketMessageChannel _destChannel;
        private readonly SocketUser _user;
        private readonly StatsManager _statsManager;

        public RockPaperScissorsCommand(string[] msgContents, SocketMessage socketMessage)
        {
            _rnd = new();
            _choices = CreateChoicesDictionary();
            _strChoices = CreateStrChoicesDictionary();
            
            if (msgContents.Length > 1)
                _userChoice = msgContents[1].ToLower();
            else
                _userChoice = ""; // error out case...
            
            _destChannel = socketMessage.Channel;
            _user = socketMessage.Author;
            _statsManager = StatsManager.Instance;
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
                await DoGameAsync();
        }

        private bool ValidUserChoice()
        {
            return _strChoices.ContainsKey(_userChoice.ToLower());
        }

        private async Task SendErrorMessageAsync()
        {
            await _destChannel.SendMessageAsync("That's not a rock, paper, or scissors!");
        }

        private async Task DoGameAsync()
        {
            var bot = CreateBotPlayer();
            var user = CreateUserPlayer();
            var gameResult = GetGameResult(bot, user);
            await SendGameResultAsync(gameResult, bot: bot, user: user);
            _statsManager.Update(gameResult);
        }

        private async Task SendGameResultAsync(GameResult gameResult, RPSPlayer bot, RPSPlayer user)
        {
            var nullableWinner = gameResult.GetWinner();
            var resultStr = nullableWinner is not null ? $"Result: {nullableWinner.Name} Wins!" : "Result: Tie";
            await _destChannel.SendMessageAsync($"User: {user.Type}, Bot: {bot.Type}, {resultStr}");
        }

        private RPSPlayer CreateBotPlayer()
        {
            int botChoice = CreateRandomBotChoice();
            var botChoiceAsType = _choices[botChoice];
            return new RPSPlayer(botChoiceAsType, "Bot", RPSPlayer.BOT_ID);
        }

        private int CreateRandomBotChoice()
        {
            return _rnd.Next(Enum.GetNames(typeof(RPSType)).Length);
        }

        private RPSPlayer CreateUserPlayer()
        {
            var userChoiceAsType = _strChoices[_userChoice];
            return new RPSPlayer(userChoiceAsType, _user.Username, _user.Id);
        }

        private static GameResult GetGameResult(RPSPlayer first, RPSPlayer second)
        {
            var compare = first.Type.Compare(second.Type);

            if (compare == -1)
                return new GameResult(first, second, GameResultType.P2);
            else if (compare == 1)
                return new GameResult(first, second, GameResultType.P1);
            else // 0 == tie
                return new GameResult(first, second, GameResultType.Tie);
        }
    }
}
