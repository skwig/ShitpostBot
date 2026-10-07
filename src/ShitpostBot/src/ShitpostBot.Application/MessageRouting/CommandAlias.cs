using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.MessageRouting;

/// <summary>Forwards an alias to a command, optionally preserving trailing arguments.</summary>
public sealed class CommandAlias(
    IChatClient chatClient,
    BotCommandFeature target,
    string alias,
    string canonical,
    bool forwardArguments = false
) : BotCommandFeature(chatClient)
{
    public override string? HelpMessage =>
        $"`{alias}{(forwardArguments ? " [arguments]" : "")}` - alias for `{canonical}`";

    protected override Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        var exactMatch = command == alias;
        var argumentMatch =
            forwardArguments
            && command.StartsWith(alias, StringComparison.Ordinal)
            && command.Length > alias.Length
            && char.IsWhiteSpace(command[alias.Length]);
        if (!exactMatch && !argumentMatch)
        {
            return Task.FromResult(false);
        }

        var expandedCommand = canonical + command[alias.Length..];
        return target.TryHandleAliasedCommand(
            commandMessageIdentification,
            expandedCommand,
            referenced,
            EditBotResponseMessageId,
            ct
        );
    }
}
