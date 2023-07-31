using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot;

internal class Program
{
    private readonly DiscordSocketClient _client;
    private readonly string _adminId;
    private readonly string _token;
    private const int MessageCacheSize = 100;

    public static async Task Main() => await new Program().StartupAsync();

    private Program()
    {
        _token = ReadTokenFile();
        _adminId = ReadAdminIdFile();
        _client = CreateDiscordSocketClient();
        SubscribeEventHandlers();
    }

    // for local runs:
    // @"<full-path-to>\token.txt"
    // for docker builds:
    // "token.txt"
    private static string ReadTokenFile() => File.ReadAllText("token.txt");

    // for local runs:
    // @"<full-path-to>\admin_id.txt"
    // for docker builds:
    // "admin_id.txt"
    private static string ReadAdminIdFile() => File.ReadAllText("admin_id.txt");

    private static DiscordSocketClient CreateDiscordSocketClient() =>
        new(new DiscordSocketConfig
        {
            MessageCacheSize = MessageCacheSize,
            GatewayIntents = GatewayIntents.All
        });

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

    private bool IsAdminMessage(SocketMessage socketMessage) => _adminId == socketMessage.Id.ToString();
}