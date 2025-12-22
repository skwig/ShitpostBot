using Microsoft.EntityFrameworkCore;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure;

internal class DbMigrator(ShitpostBotDbContext dbContext) : IDbMigrator
{
    public Task MigrateAsync(TimeSpan? commandTimeout, CancellationToken cancellationToken)
    {
        if (commandTimeout != null)
        {
            dbContext.Database.SetCommandTimeout(commandTimeout.Value);
        }

        return dbContext.Database.MigrateAsync(cancellationToken);
    }
}