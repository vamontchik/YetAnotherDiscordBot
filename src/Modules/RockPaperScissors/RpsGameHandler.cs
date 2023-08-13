using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot.Modules.RockPaperScissors;

public sealed class RpsGameHandler
{
    private readonly Random _random = new();

    private readonly Dictionary<int, RpsType> _choices = new()
    {
        { 0, RpsType.Rock },
        { 1, RpsType.Paper },
        { 2, RpsType.Scissors }
    };

    private readonly Dictionary<string, RpsType> _strChoices = new()
    {
        { "rock", RpsType.Rock },
        { "paper", RpsType.Paper },
        { "scissors", RpsType.Scissors }
    };

    public required string Argument { get; init; }
    public required SocketCommandContext SocketCommandContext { get; init; }
    public required IStatsManager StatsManager { get; init; }

    public bool ValidUserChoice() => _strChoices.ContainsKey(Argument);
    
    public async Task DoGameAsync()
    {
        var bot = await CreateBotPlayerAsync();
        var player = await CreateUserPlayerAsync();
        var gameResult = await CalculateGameResultAsync(bot, player);
        await SendGameResultAsync(gameResult, bot, player);
        await StatsManager.UpdateAsync(gameResult);
    }
    
    private Task<RpsPlayer> CreateBotPlayerAsync()
    {
        var botChoice = _random.Next(Enum.GetNames(typeof(RpsType)).Length);
        var botChoiceAsType = _choices[botChoice];
        return Task.FromResult(new RpsPlayer
        {
            Id = RpsPlayer.BotId,
            Name = "Bot",
            Type = botChoiceAsType
        });
    }

    private Task<RpsPlayer> CreateUserPlayerAsync()
    {
        var userChoiceAsType = _strChoices[Argument];
        var userPlayerInfo = SocketCommandContext.User;
        return Task.FromResult(new RpsPlayer
        {
            Id = userPlayerInfo.Id,
            Name = userPlayerInfo.Username,
            Type = userChoiceAsType
        });
    }

    private static Task<GameResult> CalculateGameResultAsync(RpsPlayer first, RpsPlayer second) =>
        Task.FromResult(first.Type.Compare(second.Type) switch
        {
            -1 => new GameResult { P1 = first, P2 = second, WinType = GameResultType.P2 },
            1 => new GameResult { P1 = first, P2 = second, WinType = GameResultType.P1 },
            _ => new GameResult { P1 = first, P2 = second, WinType = GameResultType.Tie }
        });
    
    private async Task SendGameResultAsync(GameResult gameResult, RpsPlayer bot, RpsPlayer user)
    {
        var winner = await gameResult.GetWinnerAsync();
        var resultStr = winner is not null ? $"Result: {winner.Name} Wins!" : "Result: Tie";
        await SocketCommandContext.Message.ReplyAsync($"User: {user.Type}, Bot: {bot.Type}, {resultStr}");
    }
}