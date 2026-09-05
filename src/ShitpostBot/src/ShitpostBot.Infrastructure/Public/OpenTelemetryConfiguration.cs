using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ShitpostBot.Infrastructure;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddShitpostBotOpenTelemetry(
        this IServiceCollection services,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null
    )
    {
        var openTelemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(
                    serviceName,
                    serviceVersion: typeof(OpenTelemetryConfiguration)
                        .Assembly.GetName()
                        .Version?.ToString()
                        ?? "dev"
                )
            )
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ShitpostBotActivitySource.Name)
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                configureTracing?.Invoke(tracing);
            });

        if (IsOtlpExporterConfigured())
        {
            openTelemetry.UseOtlpExporter();
        }

        return services;
    }

    public static bool IsOtlpExporterConfigured()
    {
        return HasValue("OTEL_EXPORTER_OTLP_ENDPOINT")
            || HasValue("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");
    }

    private static bool HasValue(string environmentVariable)
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentVariable));
    }
}
