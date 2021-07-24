using System.Threading.Tasks;

namespace DiscordBot
{
    internal interface ICommand
    {
        public Task ExecuteAsync();
    }
}
