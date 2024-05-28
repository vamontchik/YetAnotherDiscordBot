using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Handler;

public sealed class PrefixHandler(
    DiscordSocketClient client,
    CommandService commands,
    IConfigurationRoot configuration,
    IServiceProvider services)
{
    public void Initialize() => client.MessageReceived += HandleCommandAsync;

    public void AddModule<T>() => commands.AddModuleAsync<T>(services);

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
            var mentionsBot = socketUserMessage.HasMentionPrefix(client.CurrentUser, ref argumentPosition);
            if (!hasCharPrefix && !mentionsBot)
                return;

            var authorIsBot = socketUserMessage.Author.IsBot;
            if (authorIsBot)
                return;

            var context = new SocketCommandContext(client, socketUserMessage);
            await commands.ExecuteAsync(context, argumentPosition, services);
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

        _prefixChar = configuration["prefix"]?[0] ?? char.MinValue;
    }

    private char _prefixChar = char.MinValue;
}