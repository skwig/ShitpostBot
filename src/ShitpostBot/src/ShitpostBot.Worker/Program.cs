using Microsoft.Extensions.Diagnostics.HealthChecks;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.Repost;
using ShitpostBot.Infrastructure;
using ShitpostBot.Worker;
using ShitpostBot.Worker.Public;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddShitpostBotApplication(hostContext.Configuration);
    services.AddShitpostBotInfrastructure(hostContext.Configuration);
    services.AddDiscordClient(hostContext.Configuration);
    services.AddShitpostBotMassTransit(hostContext.Configuration, x =>
    {
        x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
        x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
    });

    services.AddShitpostBotWorker();

    services.AddHealthChecks().AddCheck<DefaultHealthCheck>("default");
    services.AddHostedService<TcpHealthProbeService>();
});

builder.Build().Run();

namespace ShitpostBot.Worker
{
    public class DefaultHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy));
        }
    }
}