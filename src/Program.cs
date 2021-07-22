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
        private MessageProcessor _messageProcessor;
        private string _adminID;
        private string _token;
        private readonly int MESSAGE_CACHE_SIZE = 100;

        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            ReadAuthFiles();
            CreateMessageProcessor();
            CreateDiscordSocketClient();
            SubscribeEventHandlers();
            await LoginAsync();
            await WaitForeverAsync();
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

        private void CreateMessageProcessor()
        {
            _messageProcessor = new MessageProcessor();
        }

        private void CreateDiscordSocketClient()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = MESSAGE_CACHE_SIZE };
            _client = new DiscordSocketClient(config);
        }

        private void SubscribeEventHandlers()
        {
            _client.Log += OnLogMessageEvent;
            _client.MessageReceived += OnMessageReceived;
        }

        private async Task WaitForeverAsync()
        {
            await Task.Delay(-1);
        }

        private Task OnMessageReceived(SocketMessage socketMessage)
        {
            var msg = new Message(socketMessage, IsAdminMessage(socketMessage));
            _messageProcessor.ProcessMessage(msg);
            return Task.CompletedTask;
        }

        private bool IsAdminMessage(SocketMessage socketMessage)
        {
            return _adminID == socketMessage.Id.ToString();
        }

        private async Task LoginAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }

        private Task OnLogMessageEvent(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
