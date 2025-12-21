using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddShitpostBotInfrastructure(hostContext.Configuration);
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var dbMigrator = scope.ServiceProvider.GetRequiredService<IDbMigrator>();

logger.LogInformation("Database migration starting at: {time}", DateTimeOffset.Now);

await dbMigrator.MigrateAsync(null, CancellationToken.None);

logger.LogInformation("Database migration completed at: {time}", DateTimeOffset.Now);