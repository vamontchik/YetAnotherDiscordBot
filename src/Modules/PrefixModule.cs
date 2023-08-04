using System;
using System.Linq;
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
    public async Task HandleRpsCommand([Remainder] string choice)
    {
        var userEnteredValues = choice
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (userEnteredValues.Count > 1)
        {
            await Context.Message.ReplyAsync("Please specify only one argument");
            return;
        }

        var rpsCommand = new RockPaperScissorsCommand(userEnteredValues.First(), Context, StatsManager);

        await rpsCommand.ExecuteAsync();
    }
}