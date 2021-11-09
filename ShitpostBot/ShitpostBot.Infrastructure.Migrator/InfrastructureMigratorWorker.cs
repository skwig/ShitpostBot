using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace ShitpostBot.Infrastructure.Migrator
{
    public class InfrastructureMigratorWorker : IHostedService
    {
        private readonly ILogger<InfrastructureMigratorWorker> logger;
        private readonly IServiceScopeFactory scopeFactory;

        public InfrastructureMigratorWorker(ILogger<InfrastructureMigratorWorker> logger, IServiceScopeFactory scopeFactory)
        {
            this.logger = logger;
            this.scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("InfrastructureMigratorWorker running at: {time}", DateTimeOffset.Now);
            
            using var serviceScope = scopeFactory.CreateScope();
            var applicationLifetime = serviceScope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
            var dbMigrator = serviceScope.ServiceProvider.GetRequiredService<IDbMigrator>();
            
            var migrateDbTask = dbMigrator.MigrateAsync(null, cancellationToken);

            await migrateDbTask;

            logger.LogInformation("InfrastructureMigratorWorker ending at: {time}", DateTimeOffset.Now);

            applicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}