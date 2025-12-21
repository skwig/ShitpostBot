namespace ShitpostBot.Infrastructure.Services;

public interface IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; }
    public DateTimeOffset Now { get; }
}