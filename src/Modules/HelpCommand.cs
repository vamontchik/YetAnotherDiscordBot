using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot.Modules;

internal sealed class HelpCommand(SocketCommandContext context)
{
    private readonly IEnumerable<string> _commands =
    [
        "ping",
        "rps",
        "join",
        "leave",
        "play",
        "skip",
        "help"
    ];

    public async Task ExecuteAsync() => await context
        .Message
        .ReplyAsync($"Allowed: {string.Join(",", _commands)}")
        .ConfigureAwait(false);
}