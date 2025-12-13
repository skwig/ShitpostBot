using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShitpostBot.Infrastructure.Migrator;

public class InfrastructureMigratorWorker(ILogger<InfrastructureMigratorWorker> logger, IServiceScopeFactory factory)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("InfrastructureMigratorWorker running at: {time}", DateTimeOffset.Now);

        using var serviceScope = factory.CreateScope();
        var applicationLifetime = serviceScope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        var dbMigrator = serviceScope.ServiceProvider.GetRequiredService<IDbMigrator>();

        var migrateDbTask = dbMigrator.MigrateAsync(null, cancellationToken);

        await migrateDbTask;

        logger.LogInformation("InfrastructureMigratorWorker ending at: {time}", DateTimeOffset.Now);

        applicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}