using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private string _adminID;
        private string _token;
        private readonly int MESSAGE_CACHE_SIZE = 100;

        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            ReadAuthFiles();
            CreateDiscordSocketClient();
            SubscribeEventHandlers();
            await LoginAsync();
            await WaitForeverAsync();
        }

        private static async Task WaitForeverAsync()
        {
            await Task.Delay(-1);
        }

        private async Task LoginAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }

        private void SubscribeEventHandlers()
        {
            _client.Log += OnLogMessageEvent;
            _client.MessageReceived += OnMessageReceived;
        }

        private void ReadAuthFiles()
        {
            // for local runs:
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt"
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\id.txt"
            // for docker builds:
            // "token.txt"
            // "id.txt"

            _token = File.ReadAllText(@"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt");
            _adminID = File.ReadAllText(@"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\id.txt");
        }

        private void CreateDiscordSocketClient()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = MESSAGE_CACHE_SIZE };
            _client = new DiscordSocketClient(config);
        }

        private Task OnMessageReceived(SocketMessage socketMessage)
        {
            var userID = socketMessage.Author.Id;
            if (userID.ToString() == _adminID)
            {
                Console.WriteLine($"Admin: {socketMessage.Content}");
            }
            else
            {
                Console.WriteLine($"Non-admin: {socketMessage.Content}");
            }

            return Task.CompletedTask;
        }

        private Task OnLogMessageEvent(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            
            return Task.CompletedTask;
        }
    }
}
