using System.Threading.Tasks;
using Discord.Interactions;

namespace DiscordBot.Modules;

public sealed class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Receive a ping message")]
    public async Task HandlePingCommand()
    {
        await RespondAsync("pong");
    }
}