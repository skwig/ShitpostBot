namespace ShitpostBot.Infrastructure.Services;

public interface IMetrics
{
    DateTimeOffset DeployedOn { get; }
    DateTimeOffset? LastLinkSaveTimestamp { get; set; }
    DateTimeOffset? LastImageSaveTimestamp { get; set; }
    DateTimeOffset? LastImageEvaluationTimestamp { get; set; }
}
