using MassTransit;
using ShitpostBot.Backprocessor;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

var builder = Host.CreateDefaultBuilder(args);

builder.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.ConfigureServices(
    (hostContext, services) =>
    {
        services.AddShitpostBotInfrastructure(hostContext.Configuration);
        services.AddDiscordClient(hostContext.Configuration);
        services.AddShitpostBotMassTransit(hostContext.Configuration);

        services
            .AddOptions<BackprocessorOptions>()
            .Bind(hostContext.Configuration.GetSection("Backprocessor"))
            .ValidateDataAnnotations()
            .Validate(options => options.Channels.All(c => c.GuildId != 0), "GuildId is required")
            .Validate(
                options => options.Channels.All(c => c.ChannelId != 0),
                "ChannelId is required"
            )
            .ValidateOnStart();

        services.AddSingleton<IBackprocessorStateStore, JsonBackprocessorStateStore>();
        services.AddSingleton<IDiscordHistoryClient, DiscordHistoryClient>();
        services.AddScoped<ImageBackfillService>();
        services.AddScoped<BackprocessorRunner>();
    }
);

var host = builder.Build();

using var scope = host.Services.CreateScope();
var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
await chatClient.ConnectAsync();

var runner = scope.ServiceProvider.GetRequiredService<BackprocessorRunner>();
await runner.RunAsync();
