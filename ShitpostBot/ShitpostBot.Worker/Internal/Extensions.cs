using System;
using ShitpostBot.Domain;

namespace ShitpostBot.Worker;

public static class Extensions
{
    public static Uri GetUri(this ChatMessageIdentifier identifier)
    {
        return new Uri($"https://discordapp.com/channels/{identifier.GuildId}/{identifier.ChannelId}/{identifier.MessageId}");
    }
}