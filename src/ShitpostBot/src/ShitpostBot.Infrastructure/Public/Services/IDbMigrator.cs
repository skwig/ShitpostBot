namespace ShitpostBot.Infrastructure.Services;

public interface IDbMigrator
{
    /// <summary>
    /// Applies migrations to a database
    /// </summary>
    /// <param name="commandTimeout">if null default command timeout of the database is used</param>
    Task MigrateAsync(TimeSpan? commandTimeout = default, CancellationToken cancellationToken = default);
}