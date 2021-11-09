using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ShitpostBot.Infrastructure
{
    internal class DbMigrator : IDbMigrator
    {
        private IDbContextFactory<ShitpostBotDbContext> ContextFactory { get; }

        public DbMigrator(IDbContextFactory<ShitpostBotDbContext> contextFactory)
        {
            ContextFactory = contextFactory;
        }
        
        public Task MigrateAsync(TimeSpan? commandTimeout, CancellationToken cancellationToken)
        {
            var context = ContextFactory.CreateDbContext();
            
            if (commandTimeout != null)
            {
                context.Database.SetCommandTimeout(commandTimeout.Value);    
            }
            
            return context.Database.MigrateAsync(cancellationToken);
        }
    }
}