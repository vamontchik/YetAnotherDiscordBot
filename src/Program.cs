using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly MessageProcessor _messageProcessor;
        private readonly string _adminID;
        private readonly string _token;
        private const int MESSAGE_CACHE_SIZE = 100;

        public static void Main() => new Program().StartupAsync().Wait();

        private Program()
        {
            _token = ReadTokenFile();
            _adminID = ReadAdminIDFile();
            _messageProcessor = CreateMessageProcessor();
            _client = CreateDiscordSocketClient();
            SubscribeEventHandlers();
        }

        private static string ReadTokenFile()
        {
            // for local runs:
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt"
            // for docker builds:
            // "token.txt"

            return File.ReadAllText(@"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt");
        }

        private static string ReadAdminIDFile()
        {
            // for local runs:
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\id.txt"
            // for docker builds:
            // "id.txt"

            return File.ReadAllText(@"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\id.txt");
        }

        private static MessageProcessor CreateMessageProcessor()
        {
            return new MessageProcessor();
        }

        private static DiscordSocketClient CreateDiscordSocketClient()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = MESSAGE_CACHE_SIZE };
            return new DiscordSocketClient(config);
        }

        private void SubscribeEventHandlers()
        {
            _client.Log += OnLogMessageEvent;
            _client.MessageReceived += OnMessageReceived;
        }

        private async Task StartupAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        /////////////
        ///////////// EVENTS
        /////////////
        ///
        private Task OnLogMessageEvent(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        private async Task OnMessageReceived(SocketMessage socketMessage)
        {
            var msg = new Message(socketMessage, IsAdminMessage(socketMessage));
            await _messageProcessor.ProcessMessage(msg);
        }

        private bool IsAdminMessage(SocketMessage socketMessage)
        {
            return _adminID == socketMessage.Id.ToString();
        }
    }
}
