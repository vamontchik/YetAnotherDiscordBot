using System.Threading.Tasks;

namespace DiscordBot
{
    internal class EmptyCommand : ICommand
    {
        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }
}
