using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker;

public class ActivityHealthCheck(
    IMetrics metrics,
    ILogger<ActivityHealthCheck> logger,
    IDateTimeProvider dateTimeProvider
) : IHealthCheck
{
    private static readonly TimeSpan StartupGracePeriod = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxIdleDuration = TimeSpan.FromHours(12);

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var utcNow = dateTimeProvider.UtcNow;

        if (utcNow - metrics.DeployedOn < StartupGracePeriod)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Startup grace period"));
        }

        var linkStale =
            metrics.LastLinkSaveTimestamp is null
            || utcNow - metrics.LastLinkSaveTimestamp.Value > MaxIdleDuration;
        var imageStale =
            metrics.LastImageSaveTimestamp is null
            || utcNow - metrics.LastImageSaveTimestamp.Value > MaxIdleDuration;

        if (linkStale && imageStale)
        {
            logger.LogError(
                "No link or image saved for over {MaxIdleHours} hours. LastLinkSaveTimestamp: {LastLinkSave}, LastImageSaveTimestamp: {LastImageSave}",
                MaxIdleDuration.TotalHours,
                metrics.LastLinkSaveTimestamp?.ToString("O") ?? "never",
                metrics.LastImageSaveTimestamp?.ToString("O") ?? "never"
            );

            return Task.FromResult(HealthCheckResult.Unhealthy("No activity for over 12 hours"));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}