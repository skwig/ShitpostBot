using System.Runtime.CompilerServices;
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
        }, ServiceLifetime.Transient); // Transient is important


        serviceCollection.AddScoped<IDbContextFactory<ShitpostBotDbContext>, DbContextFactory<ShitpostBotDbContext>>();

        serviceCollection.AddScoped<IImagePostsReader, ImagePostsReader>();
        serviceCollection.AddScoped<ILinkPostsReader, LinkPostsReader>();
        serviceCollection.AddScoped<IPostsReader, PostsReader>();

        serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();

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
}