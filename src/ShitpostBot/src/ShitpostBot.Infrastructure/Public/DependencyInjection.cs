using System.Runtime.CompilerServices;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Grpc.Net.Client;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure.Internal.Services;
using ShitpostBot.Infrastructure.Services;

[assembly: InternalsVisibleTo("ShitpostBot.Tools")]

namespace ShitpostBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotInfrastructure(
        this IServiceCollection serviceCollection,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("ShitpostBotDatabase")
            ?? throw new ArgumentNullException();
        serviceCollection.AddDbContext<ShitpostBotDbContext>(builder =>
        {
            builder
                .UseNpgsql(
                    connectionString,
                    sqlOpts =>
                        sqlOpts
                            .MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)
                            .UseVector()
                )
                .EnableDetailedErrors();
        });

        serviceCollection.AddScoped<IDbContext>(provider =>
            provider.GetRequiredService<ShitpostBotDbContext>()
        );
        serviceCollection.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<ShitpostBotDbContext>()
        );

        serviceCollection.AddScoped<IDbMigrator, DbMigrator>();

        serviceCollection.Configure<RepostServiceOptions>(
            configuration.GetSection("RepostOptions")
        );

        serviceCollection.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        serviceCollection.AddSingleton<IMetrics, BotMetrics>();

        serviceCollection.AddMemoryCache();

        serviceCollection
            .AddOptions<ImageFeatureExtractorApiOptions>()
            .Bind(configuration.GetSection("ImageFeatureExtractorApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        serviceCollection
            .AddGrpcClient<ImageFeatureExtractor.ImageFeatureExtractorClient>(options =>
            {
                var serviceProvider = serviceCollection.BuildServiceProvider();
                var apiOptions = serviceProvider.GetRequiredService<IOptions<ImageFeatureExtractorApiOptions>>().Value;
                options.Address = new Uri(apiOptions.Uri);
            });

        return serviceCollection;
    }

    public static IServiceCollection AddDiscordClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<DiscordChatClientOptions>(configuration.GetSection("Discord"));
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DiscordChatClientOptions>>();
            return new DiscordClient(
                new DiscordConfiguration
                {
                    Token = options.Value.Token,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.All,

                    MessageCacheSize = 2048,
                }
            );
        });

        services.AddSingleton<IChatClient, DiscordChatClient>();

        return services;
    }
}