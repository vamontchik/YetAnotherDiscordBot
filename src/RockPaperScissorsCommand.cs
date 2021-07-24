using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class RockPaperScissorsCommand : ICommand
    {
        private readonly Random _rnd;
        private readonly Dictionary<int, RpsType> _choices;
        private readonly Dictionary<string, RpsType> _strChoices;
        private readonly string _userChoice;
        private readonly ISocketMessageChannel _destChannel;
        private readonly SocketUser _user;
        private readonly StatsManager _statsManager;

        public RockPaperScissorsCommand(IReadOnlyList<string> msgContents, SocketMessage socketMessage)
        {
            _rnd = new Random();
            _choices = CreateChoicesDictionary();
            _strChoices = CreateStrChoicesDictionary();
            
            _userChoice = msgContents.Count > 1 ? msgContents[1].ToLower() : "";
            
            _destChannel = socketMessage.Channel;
            _user = socketMessage.Author;
            _statsManager = StatsManager.Instance;
            

        }

        private static Dictionary<int, RpsType> CreateChoicesDictionary()
        {
            return new()
            {
                { 0, RpsType.Rock },
                { 1, RpsType.Paper },
                { 2, RpsType.Scissors }
            };
        }

        private static Dictionary<string, RpsType> CreateStrChoicesDictionary()
        {
            return new()
            {
                { "rock", RpsType.Rock },
                { "paper", RpsType.Paper },
                { "scissors", RpsType.Scissors }
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
            return _strChoices.ContainsKey(_userChoice);
        }

        private async Task SendErrorMessageAsync()
        {
            await _destChannel.SendMessageAsync("That's not a rock, paper, or scissors!");
        }

        private async Task DoGameAsync()
        {
            var bot = CreateBotPlayer();
            var player = CreateUserPlayer();
            var gameResult = GetGameResult(bot, player);
            await SendGameResultAsync(gameResult, bot, player);
            _statsManager.Update(gameResult);
        }

        private async Task SendGameResultAsync(GameResult gameResult, RpsPlayer bot, RpsPlayer user)
        {
            var nullableWinner = gameResult.GetWinner();
            var resultStr = nullableWinner is not null ? $"Result: {nullableWinner.Name} Wins!" : "Result: Tie";
            await _destChannel.SendMessageAsync($"User: {user.Type}, Bot: {bot.Type}, {resultStr}");
        }

        private RpsPlayer CreateBotPlayer()
        {
            var botChoice = CreateRandomBotChoice();
            var botChoiceAsType = _choices[botChoice];
            return new RpsPlayer(botChoiceAsType, "Bot", RpsPlayer.BotId);
        }

        private int CreateRandomBotChoice()
        {
            return _rnd.Next(Enum.GetNames(typeof(RpsType)).Length);
        }

        private RpsPlayer CreateUserPlayer()
        {
            var userChoiceAsType = _strChoices[_userChoice];
            return new RpsPlayer(userChoiceAsType, _user.Username, _user.Id);
        }

        private static GameResult GetGameResult(RpsPlayer first, RpsPlayer second)
        {
            var compare = first.Type.Compare(second.Type);

            return compare switch
            {
                -1 => new GameResult(first, second, GameResultType.P2),
                1 => new GameResult(first, second, GameResultType.P1),
                _ => new GameResult(first, second, GameResultType.Tie)
            };
        }
    }
}
