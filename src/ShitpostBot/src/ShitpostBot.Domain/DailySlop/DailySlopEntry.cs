using System;
using CSharpFunctionalExtensions;

namespace ShitpostBot.Domain;

public abstract record DailySlopScore;

public class DailySlopEntry : Entity<long>
{
    public ulong PosterId { get; private set; }
    public string GameId { get; private set; }
    public DateTimeOffset PostedOn { get; private set; }
    public ulong ChatGuildId { get; private set; }
    public ulong ChatChannelId { get; private set; }
    public ulong ChatMessageId { get; private set; }
    public DateTimeOffset TrackedOn { get; private set; }
    public DailySlopScore? Score { get; private set; }

    private DailySlopEntry()
    {
        GameId = null!;
    }

    public DailySlopEntry(
        ulong posterId,
        string gameId,
        DateTimeOffset postedOn,
        DateTimeOffset trackedOn,
        ulong chatGuildId,
        ulong chatChannelId,
        ulong chatMessageId,
        DailySlopScore? score
    )
    {
        PosterId = posterId;
        GameId = gameId;
        PostedOn = postedOn;
        TrackedOn = trackedOn;
        ChatGuildId = chatGuildId;
        ChatChannelId = chatChannelId;
        ChatMessageId = chatMessageId;
        Score = score;
    }
}
