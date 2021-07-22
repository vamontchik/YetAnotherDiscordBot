using System.Threading.Tasks;

namespace DiscordBot.src
{
    interface ICommand
    {
        public Task ExecuteAsync();
    }
}
