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

        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            // for local runs:
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt"
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\id.txt"
            // for docker builds:
            // "token.txt"
            // "id.txt"
            var token = File.ReadAllText(@"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt");
            _adminID = File.ReadAllText(@"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\id.txt");

            var config = new DiscordSocketConfig { MessageCacheSize = 100 };
            _client = new DiscordSocketClient(config);

            _client.Log += OnLogMessageEvent;
            
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.MessageReceived += OnMessageReceived;
            
            await Task.Delay(-1);
        }

        private Task OnMessageReceived(SocketMessage socketMessage)
        {
            var userID = socketMessage.Author.Id;
            if (userID.ToString() == _adminID)
            {
                // "authenticated"
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
