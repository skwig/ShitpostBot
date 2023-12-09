using System;

namespace ShitpostBot.Worker;

public interface IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; }
    public DateTimeOffset Now { get; }
}