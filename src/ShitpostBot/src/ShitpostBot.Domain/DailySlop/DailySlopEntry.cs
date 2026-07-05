using System;
using CSharpFunctionalExtensions;

namespace ShitpostBot.Domain;

public class DailySlopEntry : Entity<long>
{
    public ulong PosterId { get; private set; }
    public string GameId { get; private set; }
    public DateTimeOffset PostedOn { get; private set; }
    public ulong ChatGuildId { get; private set; }
    public ulong ChatChannelId { get; private set; }
    public ulong ChatMessageId { get; private set; }

    private DailySlopEntry()
    {
        GameId = null!;
    }

    public DailySlopEntry(
        ulong posterId,
        string gameId,
        DateTimeOffset postedOn,
        ulong chatGuildId,
        ulong chatChannelId,
        ulong chatMessageId
    )
    {
        PosterId = posterId;
        GameId = gameId;
        PostedOn = postedOn;
        ChatGuildId = chatGuildId;
        ChatChannelId = chatChannelId;
        ChatMessageId = chatMessageId;
    }
}
