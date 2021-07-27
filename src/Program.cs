using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly string _adminId;
        private readonly string _token;
        private const int MessageCacheSize = 100;

        public static void Main() => new Program().StartupAsync().Wait();

        private Program()
        {
            _token = ReadTokenFile();
            _adminId = ReadAdminIdFile();
            _client = CreateDiscordSocketClient();
            SubscribeEventHandlers();
        }

        #region Constructor Helpers        

        private static string ReadTokenFile()
        {
            // for local runs:
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\token.txt"
            // for docker builds:
            // "token.txt"

            return File.ReadAllText("token.txt");
        }

        private static string ReadAdminIdFile()
        {
            // for local runs:
            // @"C:\Users\woofers\source\repos\DiscordBot\DiscordBot\id.txt"
            // for docker builds:
            // "id.txt"

            return File.ReadAllText("id.txt");
        }

        private static DiscordSocketClient CreateDiscordSocketClient()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = MessageCacheSize };
            return new DiscordSocketClient(config);
        }

        private void SubscribeEventHandlers()
        {
            _client.Log += OnLogMessageEvent;
            _client.MessageReceived += OnMessageReceived;
        }
        
        #endregion

        private async Task StartupAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        #region Events
        
        private static Task OnLogMessageEvent(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(SocketMessage socketMessage)
        {
            var msg = new Message(socketMessage, IsAdminMessage(socketMessage));
            await MessageProcessor.Instance.ProcessMessage(msg);
        }

        #endregion

        #region Util

        private bool IsAdminMessage(SocketMessage socketMessage)
        {
            return _adminId == socketMessage.Id.ToString();
        }

        #endregion
    }
}
