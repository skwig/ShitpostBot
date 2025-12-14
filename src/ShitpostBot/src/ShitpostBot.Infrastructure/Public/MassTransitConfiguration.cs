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
        Action<IRegistrationConfigurator> configureConsumers)
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
            configureConsumers(x);
            
            x.UsingPostgres((context, cfg) =>
            {
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
