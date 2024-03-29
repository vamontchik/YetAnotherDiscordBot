﻿using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot.Modules.RockPaperScissors;

public sealed class StatsCommand
{
    public required SocketCommandContext SocketCommandContext { get; init; }
    public required IStatsManager StatsManager { get; init; }

    public async Task DoAsync()
    {
        var user = SocketCommandContext.User;
        var stat = await StatsManager.GetStatAsync(user.Id);
        if (stat is null)
        {
            await SocketCommandContext
                .Message
                .ReplyAsync($"No stats found for {user.Username}");
        }
        else
        {
            var statsStr = await stat.ComputeStatsAsync();
            await SocketCommandContext.Message.ReplyAsync($"{statsStr}");
        }
    }
}