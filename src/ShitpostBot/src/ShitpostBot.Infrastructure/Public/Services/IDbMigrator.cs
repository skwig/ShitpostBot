using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShitpostBot.Infrastructure;

public interface IDbMigrator
{
    /// <summary>
    /// Applies migrations to a database
    /// </summary>
    /// <param name="commandTimeout">if null default command timeout of the database is used</param>
    Task MigrateAsync(TimeSpan? commandTimeout = default, CancellationToken cancellationToken = default);
}