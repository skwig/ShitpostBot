using DSharpPlus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.Repost;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using ShitpostBot.Worker.Public;

namespace ShitpostBot.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                    optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
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
}

public class DefaultHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy));
    }
}