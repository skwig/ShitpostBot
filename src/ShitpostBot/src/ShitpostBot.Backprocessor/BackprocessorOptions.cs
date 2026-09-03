using System.ComponentModel.DataAnnotations;

namespace ShitpostBot.Backprocessor;

public class BackprocessorOptions
{
    [Required]
    public required string StateFilePath { get; init; }

    [Range(1, 100)]
    public int PageSize { get; init; } = 50;

    public TimeSpan PageDelay { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan MessageDelay { get; init; } = TimeSpan.FromSeconds(1);

    public IReadOnlyList<BackprocessorChannelOptions> Channels { get; init; } = [];
}

public class BackprocessorChannelOptions
{
    [Required]
    public required string Name { get; init; }

    public ulong GuildId { get; init; }

    public ulong ChannelId { get; init; }

    public ulong? OldestMessageId { get; init; }

    public ulong? StartBeforeMessageId { get; init; }
}
