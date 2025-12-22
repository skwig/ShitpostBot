using System.Runtime.CompilerServices;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure.Services;

[assembly: InternalsVisibleTo("ShitpostBot.Tools")]

namespace ShitpostBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotInfrastructure(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ShitpostBotDatabase") ??
                               throw new ArgumentNullException();
        serviceCollection.AddDbContext<ShitpostBotDbContext>(builder =>
        {
            builder
                .UseNpgsql(connectionString, sqlOpts => sqlOpts
                    .MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)
                    .UseVector()
                )
                .EnableDetailedErrors();
        });


        serviceCollection.AddScoped<IDbContext>(provider => provider.GetRequiredService<ShitpostBotDbContext>());
        serviceCollection.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ShitpostBotDbContext>());

        serviceCollection.AddScoped<IDbMigrator, DbMigrator>();

        serviceCollection.Configure<RepostServiceOptions>(configuration.GetSection("RepostOptions"));

        serviceCollection.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        serviceCollection.AddMemoryCache();

        serviceCollection.AddOptions<ImageFeatureExtractorApiOptions>()
            .Bind(configuration.GetSection("ImageFeatureExtractorApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        serviceCollection.AddRefitClient<IImageFeatureExtractorApi>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ImageFeatureExtractorApiOptions>>().Value;
                client.BaseAddress = new Uri(options.Uri);
            });

        return serviceCollection;
    }

    public static IServiceCollection AddDiscordClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DiscordChatClientOptions>(configuration.GetSection("Discord"));
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

        return services;
    }
}