using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure;

internal class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset Now => DateTimeOffset.Now;
}