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

        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        { 
            // for local runs: C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt
            var token = File.ReadAllText("token.txt");

            var config = new DiscordSocketConfig { MessageCacheSize = 100 };
            _client = new DiscordSocketClient(config);

            _client.Log += OnLogMessageEvent;
            
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.MessageReceived += OnMessageReceived;
            _client.MessageUpdated += OnMessageUpdatedAsync;
            
            await Task.Delay(-1);
        }

        private async Task OnMessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {   
            // NOTE: If the message was not in the cache,
            //       downloading it will result in getting a copy of `after`.
            var msg = await before.GetOrDownloadAsync();

            var afterTimestamp = after.Timestamp;
            var author = msg.Author;
            var msgChannel = msg.Channel;
            var beforeContent = msg.Content;
            var afterContent = after.Content;

            Console.WriteLine($"[{afterTimestamp}]\tAuthor: {author}\tChannel: {msgChannel}");
            Console.WriteLine($"{beforeContent} -> {afterContent}");
        }

        private Task OnMessageReceived(SocketMessage arg)
        {
            var author = arg.Author;
            var channel = arg.Channel;
            var content = arg.Content;
            var timestamp = arg.Timestamp;
            
            Console.WriteLine($"[{timestamp}]\tAuthor: {author}\tChannel: {channel}");
            Console.WriteLine($"{content}");

            // DO THE THING :D

            return Task.CompletedTask;
        }

        private Task OnLogMessageEvent(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            
            return Task.CompletedTask;
        }
    }
}
