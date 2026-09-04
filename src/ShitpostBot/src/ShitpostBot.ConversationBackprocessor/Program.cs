using Microsoft.Extensions.Options;
using ShitpostBot.Application.Features.ConversationSearch;
using ShitpostBot.ConversationBackprocessor;
using ShitpostBot.Infrastructure;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration(configuration =>
{
    configuration.AddJsonFile(
        Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
        optional: false,
        reloadOnChange: false
    );
});

builder.ConfigureServices(
    (hostContext, services) =>
    {
        services.AddShitpostBotInfrastructure(hostContext.Configuration);
        services.AddShitpostBotMassTransit(hostContext.Configuration);
        services.AddSingleton<ConversationFragmentStage>();
        services
            .AddOptions<ConversationBackprocessorOptions>()
            .Bind(hostContext.Configuration.GetSection("ConversationBackprocessor"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<ConversationDumpBackprocessor>();
    }
);

using var host = builder.Build();
await host.StartAsync();

try
{
    using var scope = host.Services.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<
        IOptions<ConversationBackprocessorOptions>
    >();
    if (string.IsNullOrWhiteSpace(options.Value.InputPath))
    {
        throw new InvalidOperationException("ConversationBackprocessor:InputPath is required");
    }

    var backprocessor = scope.ServiceProvider.GetRequiredService<ConversationDumpBackprocessor>();
    await backprocessor.Run();
}
finally
{
    await host.StopAsync();
}
