using System;
using System.ComponentModel.DataAnnotations;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using CloudEventify.MassTransit;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Infrastructure;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddShitpostBotMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IRegistrationConfigurator>? configureConsumers = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("ShitpostBotMessaging")
        );

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            // Workaround for Npgsql initialization race condition with pgvector
            options.Host = builder.Host;
            options.Port = builder.Port;
            options.Username = builder.Username;
            options.Password = builder.Password;
            options.Database = builder.Database;
        });

        services.AddPostgresMigrationHostedService();

        services.AddMassTransit(x =>
        {
            configureConsumers?.Invoke(x);

            x.UsingPostgres((context, cfg) =>
            {
                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(
                        retryLimit: 3,
                        minInterval: TimeSpan.FromSeconds(10),
                        maxInterval: TimeSpan.FromSeconds(90),
                        intervalDelta: TimeSpan.FromSeconds(10)
                    );

                    r.Ignore<ValidationException>();
                    r.Ignore<ArgumentException>();
                    r.Ignore<ArgumentNullException>();
                    r.Ignore<InvalidOperationException>();

                    // Explicitly handle transient failures
                    r.Handle<HttpRequestException>();
                    r.Handle<TaskCanceledException>();
                    r.Handle<TimeoutException>();
                });

                cfg.ConfigureEndpoints(context);
                cfg.UseCloudEvents()
                    .WithTypes(map => map
                        .Map<ImagePostTracked>("imagePostTracked")
                        .Map<LinkPostTracked>("linkPostTracked")
                    );
            });
        });

        return services;
    }
}