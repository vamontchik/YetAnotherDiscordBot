using System;
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
    private readonly IServiceProvider _services;

    public PrefixHandler(
        DiscordSocketClient client,
        CommandService commands,
        IConfigurationRoot configuration,
        IServiceProvider services)
    {
        _client = client;
        _commands = commands;
        _configuration = configuration;
        _services = services;
    }

    public void Initialize() => _client.MessageReceived += HandleCommandAsync;

    public void AddModule<T>() => _commands.AddModuleAsync<T>(_services);

    private async Task HandleCommandAsync(SocketMessage socketMessage)
    {
        try
        {
            if (socketMessage is not SocketUserMessage socketUserMessage)
                return;

            PopulatePrefixCharAsNecessary();
            if (_prefixChar is char.MinValue)
                return;

            var argumentPosition = 0;
            var hasCharPrefix = socketUserMessage.HasCharPrefix(_prefixChar, ref argumentPosition);
            var mentionsBot = socketUserMessage.HasMentionPrefix(_client.CurrentUser, ref argumentPosition);
            if (!hasCharPrefix && !mentionsBot)
                return;

            var authorIsBot = socketUserMessage.Author.IsBot;
            if (authorIsBot)
                return;

            var context = new SocketCommandContext(_client, socketUserMessage);
            await _commands.ExecuteAsync(context, argumentPosition, _services);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void PopulatePrefixCharAsNecessary()
    {
        if (_prefixChar is not char.MinValue)
            return;

        _prefixChar = _configuration["prefix"]?[0] ?? char.MinValue;
    }

    private char _prefixChar = char.MinValue;
}