using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Handler;

internal class PrefixHandler
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IConfigurationRoot _configuration;
    
    public PrefixHandler(
        DiscordSocketClient client,
        CommandService commands,
        IConfigurationRoot configuration)
    {
        _client = client;
        _commands = commands;
        _configuration = configuration;
    }

    public void Initialize() => _client.MessageReceived += HandleCommandAsync;

    public void AddModule<T>() => _commands.AddModuleAsync<T>(null);

    private async Task HandleCommandAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage socketUserMessage) 
            return;

        var argumentPosition = 0;
        
        var hasCharPrefix = socketUserMessage.HasCharPrefix(_configuration["prefix"]![0], ref argumentPosition);
        var mentionsBot = socketUserMessage.HasMentionPrefix(_client.CurrentUser, ref argumentPosition);
        if (!hasCharPrefix && !mentionsBot)
            return;

        var authorIsBot = socketUserMessage.Author.IsBot;
        if (authorIsBot)
            return;

        var context = new SocketCommandContext(_client, socketUserMessage);

        await _commands.ExecuteAsync(context, argumentPosition, services: null);
    }
}