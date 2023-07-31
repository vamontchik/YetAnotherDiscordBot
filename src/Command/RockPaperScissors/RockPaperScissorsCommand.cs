using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot.Command.RockPaperScissors;

internal class RockPaperScissorsCommand : ICommand
{
    private readonly Random _rnd;
    private readonly Dictionary<int, RpsType> _choices;
    private readonly Dictionary<string, RpsType> _strChoices;
    private readonly string _arg;
    private readonly bool _isStatCheck;
    private readonly ISocketMessageChannel _destChannel;
    private readonly SocketUser _user;
    private readonly StatsManager _statsManager;
    private readonly SocketMessage _socketMessage;

    public RockPaperScissorsCommand(IReadOnlyList<string> msgContents, SocketMessage socketMessage)
    {
        _rnd = new Random();
        _choices = CreateChoicesDictionary();
        _strChoices = CreateStrChoicesDictionary();

        _arg = msgContents.Count > 1 ? msgContents[1].ToLower() : string.Empty;
        _isStatCheck = _arg.Equals("stats");

        _destChannel = socketMessage.Channel;
        _user = socketMessage.Author;
        _statsManager = StatsManager.Instance;

        _socketMessage = socketMessage;
    }

    private static Dictionary<int, RpsType> CreateChoicesDictionary() => new()
    {
        { 0, RpsType.Rock },
        { 1, RpsType.Paper },
        { 2, RpsType.Scissors }
    };

    private static Dictionary<string, RpsType> CreateStrChoicesDictionary() => new()
    {
        { "rock", RpsType.Rock },
        { "paper", RpsType.Paper },
        { "scissors", RpsType.Scissors }
    };

    public async Task ExecuteAsync()
    {
        if (_isStatCheck)
            await new StatsEntryPoint(_socketMessage).DoAsync();
        else if (!ValidUserChoice())
            await SendErrorMessageAsync();
        else
            await DoGameAsync();
    }

    private bool ValidUserChoice() => _strChoices.ContainsKey(_arg);

    private async Task SendErrorMessageAsync() =>
        await _destChannel.SendMessageAsync("That's not a rock, paper, or scissors!");

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
        var winner = gameResult.GetWinner();
        var resultStr = winner is not null ? $"Result: {winner.Name} Wins!" : "Result: Tie";
        await _destChannel.SendMessageAsync($"User: {user.Type}, Bot: {bot.Type}, {resultStr}");
    }

    private RpsPlayer CreateBotPlayer()
    {
        var botChoice = CreateRandomBotChoice();
        var botChoiceAsType = _choices[botChoice];
        return new RpsPlayer(botChoiceAsType, "Bot", RpsPlayer.BotId);
    }

    private int CreateRandomBotChoice() => _rnd.Next(Enum.GetNames(typeof(RpsType)).Length);

    private RpsPlayer CreateUserPlayer()
    {
        var userChoiceAsType = _strChoices[_arg];
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