using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CloudEventify.MassTransit;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Worker.Features.Repost;
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
                var builder = new NpgsqlConnectionStringBuilder(
                    hostContext.Configuration.GetConnectionString("ShitpostBotMessaging")
                );
                services.AddOptions<SqlTransportOptions>().Configure(options =>
                {
                    // For whatever reason this doesn't work. On insert, it throws
                    // > `Writing values of 'Pgvector.Vector' is not supported for parameters having no NpgsqlDbType or DataTypeName. Try setting one of these values to the expected database type.`
                    // Filling the options one by one from the builder works for some reason.
                    // I suspect there is some weird Npgsql initialization race condition.
                    // options.ConnectionString = hostContext.Configuration.GetConnectionString("ShitpostBotMessaging")
                    //                            ?? throw new ArgumentNullException();

                    options.Host = builder.Host;
                    options.Port = builder.Port;
                    options.Username = builder.Username;
                    options.Password = builder.Password;
                    options.Database = builder.Database;
                });
                services.AddPostgresMigrationHostedService();
                services.AddMassTransit(x =>
                {
                    x.UsingPostgres((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                        cfg.UseCloudEvents()
                            .WithTypes(map => map
                                .Map<ImagePostTracked>("imagePostTracked")
                                .Map<LinkPostTracked>("linkPostTracked")
                            );
                    });
                    x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
                    x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
                });

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