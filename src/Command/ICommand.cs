using System.Threading.Tasks;

namespace DiscordBot.Command
{
    internal interface ICommand
    {
        public Task ExecuteAsync();
    }
}
