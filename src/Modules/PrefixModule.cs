using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Modules.RockPaperScissors;

namespace DiscordBot.Modules;

public class PrefixModule : ModuleBase<SocketCommandContext>
{
    public StatsManager StatsManager { get; set; }

    [Command("ping")]
    public async Task HandlePingCommand()
    {
        await Context.Message.ReplyAsync("pong");
    }

    [Command("rps")]
    public async Task HandleRpsCommandNoArg()
    {
        await Context.Message.ReplyAsync("Please specify an argument");
    }

    [Command("rps")]
    public async Task HandleRpsCommand(string choice)
    {
        var rpsCommand = new RockPaperScissorsCommand(choice, Context, StatsManager);
        await rpsCommand.ExecuteAsync();
    }
}