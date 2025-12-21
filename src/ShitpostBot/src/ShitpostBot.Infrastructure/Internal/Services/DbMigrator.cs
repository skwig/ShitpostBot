using Microsoft.EntityFrameworkCore;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure;

internal class DbMigrator(IDbContextFactory<ShitpostBotDbContext> contextFactory) : IDbMigrator
{
    private IDbContextFactory<ShitpostBotDbContext> ContextFactory { get; } = contextFactory;

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