using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot.Modules.RockPaperScissors;

public sealed class StatsEntryPoint
{
    private readonly SocketCommandContext _socketCommandContext;
    private readonly StatsManager _statsManager;

    public StatsEntryPoint(
        SocketCommandContext socketCommandContext,
        StatsManager statsManager)
    {
        _socketCommandContext = socketCommandContext;
        _statsManager = statsManager;
    }

    public async Task DoAsync()
    {
        var stat = _statsManager.GetStat(_socketCommandContext.User.Id);
        if (stat is null)
            await _socketCommandContext.Message.ReplyAsync(
                $"No stats found for {_socketCommandContext.User.Username}");
        else
        {
            var statsStr = stat.ComputeStats();
            await _socketCommandContext.Message.ReplyAsync($"{statsStr}");
        }
    }
}