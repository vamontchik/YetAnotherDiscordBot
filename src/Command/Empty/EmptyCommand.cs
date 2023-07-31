using System.Threading.Tasks;

namespace DiscordBot.Command.Empty;

internal class EmptyCommand : ICommand
{
    public Task ExecuteAsync() => Task.CompletedTask;
}