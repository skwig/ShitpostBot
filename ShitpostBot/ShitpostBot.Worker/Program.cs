using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace ShitpostBot.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseNServiceBus(hostContext =>
                {
                    var endpointConfiguration = new EndpointConfiguration("ShitpostBot.Worker");

                    endpointConfiguration.EnableInstallers();
                    endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

                    var connectionString = hostContext.Configuration.GetConnectionString("ShitpostBotMessaging") ?? throw new ArgumentNullException();
                    endpointConfiguration.UseTransport<RabbitMQTransport>()
                        .UseConventionalRoutingTopology(QueueType.Quorum)
                        .ConnectionString(connectionString);

                    return endpointConfiguration;
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddShitpostBotWorker(hostContext.Configuration);

                    services.AddHealthChecks().AddCheck<DefaultHealthCheck>("default");
                    services.AddHostedService<TcpHealthProbeService>();
                });
    }

    public class DefaultHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy));
        }
    }
}