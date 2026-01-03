using System;
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
                // Add exponential retry for transient failures
                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(
                        retryLimit: 3,
                        minInterval: TimeSpan.FromSeconds(10),
                        maxInterval: TimeSpan.FromSeconds(90),
                        intervalDelta: TimeSpan.FromSeconds(10)
                    );
                    
                    // Don't retry validation or argument errors
                    r.Ignore<System.ComponentModel.DataAnnotations.ValidationException>();
                    r.Ignore<ArgumentException>();
                    r.Ignore<ArgumentNullException>();
                    r.Ignore<InvalidOperationException>(ex => 
                        ex.Message.StartsWith("ML service client error"));
                    
                    // Explicitly handle transient failures
                    r.Handle<HttpRequestException>();    // Network/connection failures
                    r.Handle<TaskCanceledException>();   // Timeouts
                    r.Handle<TimeoutException>();        // Explicit timeouts
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