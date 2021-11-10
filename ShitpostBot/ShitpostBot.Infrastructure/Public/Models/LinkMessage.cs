using System;

namespace ShitpostBot.Infrastructure
{
    public record LinkMessage(MessageIdentification Identification, LinkMessageEmbed Embed, DateTimeOffset PostedOn);

    public record LinkMessageEmbed(Uri Uri);
}