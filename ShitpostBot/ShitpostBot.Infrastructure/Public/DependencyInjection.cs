using System;
using System.Collections.Generic;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShitpostBot.Domain;
// using ShitpostBot.Infrastructure.Migrations;
using ShitpostBot.Worker;
using Unleash;

namespace ShitpostBot.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddShitpostBotInfrastructure(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ShitpostBotDatabase") ?? throw new ArgumentNullException();
            serviceCollection.AddDbContext<ShitpostBotDbContext>(builder =>
            {
                // builder.UseSqlServer(connectionString, sqlOpts => sqlOpts.MigrationsAssembly(typeof(Initial).Assembly.FullName))
                builder.UseSqlServer(connectionString, sqlOpts => sqlOpts.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName))
                    .EnableDetailedErrors();
            }, ServiceLifetime.Transient); // Transient is important
            
            serviceCollection.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<DiscordChatClientOptions>>();
                return new DiscordClient(new DiscordConfiguration
                {
                    Token = options.Value.Token,
                    TokenType = TokenType.Bot,
                    
                    MessageCacheSize = 2048
                });
            });

            serviceCollection.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<UnleashClientOptions>>();

                if (!options.Value.Enabled)
                {
                    return new FakeUnleash();
                }
                
                var hostEnvironment = provider.GetRequiredService<IHostingEnvironment>();
                
                var settings = new UnleashSettings()
                {
                    AppName = "shitpost-bot",
                    Environment = hostEnvironment.EnvironmentName.ToLower(),
                    UnleashApi = new Uri(options.Value.Url),
                    CustomHttpHeaders = new Dictionary<string, string>()
                    {
                        {"Authorization",options.Value.Token }
                    }
                };
                
                return new DefaultUnleash(settings) as IUnleash;
            });

            serviceCollection.AddSingleton<IChatClient, DiscordChatClient>();
            
            serviceCollection.AddScoped<IDbContextFactory<ShitpostBotDbContext>, DbContextFactory<ShitpostBotDbContext>>();

            serviceCollection.AddScoped<IImagePostsReader, ImagePostsReader>();
            serviceCollection.AddScoped<IPostsReader, PostsReader>();
            
            serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            
            serviceCollection.AddScoped<IDbMigrator, DbMigrator>();
            
            serviceCollection.Configure<DiscordChatClientOptions>(configuration.GetSection("Discord"));
            serviceCollection.Configure<RepostServiceOptions>(configuration.GetSection("RepostOptions"));
            serviceCollection.Configure<UnleashClientOptions>(configuration.GetSection("Unleash"));
            
            serviceCollection.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            
            serviceCollection.AddMemoryCache();
            
            return serviceCollection;
        }
    }
}