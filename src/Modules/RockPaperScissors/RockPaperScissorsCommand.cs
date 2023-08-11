using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot.Modules.RockPaperScissors;

public sealed class RockPaperScissorsCommand
{
    public required string Argument { get; init; }
    public required bool IsStatCheck { get; init; }
    public required StatsManager StatsManager { get; init; }
    public required SocketCommandContext SocketCommandContext { get; init; }

    public async Task ExecuteAsync()
    {
        if (IsStatCheck)
        {
            var statsCommand = new StatsCommand
            {
                SocketCommandContext = SocketCommandContext,
                StatsManager = StatsManager
            };
            await statsCommand.DoAsync();
            return;
        }

        var gameHandler = new RpsGameHandler
        {
            Argument = Argument,
            SocketCommandContext = SocketCommandContext,
            StatsManager = StatsManager
        };
        if (!gameHandler.ValidUserChoice())
        {
            await SocketCommandContext.Message.ReplyAsync("That's not a rock, paper, or scissors!");
            return;
        }

        await gameHandler.DoGameAsync();
    }
}