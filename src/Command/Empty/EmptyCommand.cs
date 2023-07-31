using System.Threading.Tasks;

namespace DiscordBot.Command.Empty;

internal sealed class EmptyCommand : ICommand
{
    public Task ExecuteAsync() => Task.CompletedTask;
}