using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure.Internal.Services;

internal sealed class BotMetrics : IMetrics
{
    public DateTimeOffset DeployedOn { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLinkSaveTimestamp { get; set; }
    public DateTimeOffset? LastImageSaveTimestamp { get; set; }
    public DateTimeOffset? LastImageEvaluationTimestamp { get; set; }
}
