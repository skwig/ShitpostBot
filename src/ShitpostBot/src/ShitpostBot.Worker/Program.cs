using DSharpPlus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.Repost;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using ShitpostBot.Worker;
using ShitpostBot.Worker.Public;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddShitpostBotMassTransit(hostContext.Configuration, x =>
    {
        x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
        x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
    });

    services.Configure<DiscordChatClientOptions>(hostContext.Configuration.GetSection("Discord"));
    services.AddSingleton(provider =>
    {
        var options = provider.GetRequiredService<IOptions<DiscordChatClientOptions>>();
        return new DiscordClient(new DiscordConfiguration
        {
            Token = options.Value.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.All,

            MessageCacheSize = 2048
        });
    });

    services.AddSingleton<IChatClient, DiscordChatClient>();

    services.AddShitpostBotApplication(hostContext.Configuration);
    services.AddHostedService<Worker>();
    services.AddShitpostBotWorker(hostContext.Configuration);

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