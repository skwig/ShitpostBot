using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure.Migrator;

public class InfrastructureMigratorWorker(ILogger<InfrastructureMigratorWorker> logger, IServiceScopeFactory factory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InfrastructureMigratorWorker running at: {time}", DateTimeOffset.Now);

        using var serviceScope = factory.CreateScope();
        var applicationLifetime = serviceScope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        var dbMigrator = serviceScope.ServiceProvider.GetRequiredService<IDbMigrator>();

        await dbMigrator.MigrateAsync(null, stoppingToken);

        logger.LogInformation("InfrastructureMigratorWorker ending at: {time}", DateTimeOffset.Now);

        applicationLifetime.StopApplication();
    }
}