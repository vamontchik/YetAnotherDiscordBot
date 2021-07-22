using System.Threading.Tasks;

namespace DiscordBot.src
{
    class EmptyCommand : ICommand
    {
        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }
}
