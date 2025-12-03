using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ShitpostBot.Infrastructure;

internal class DbContextFactory<TDbContext>(IServiceProvider provider) : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext CreateDbContext()
    {
        return provider.GetRequiredService<TDbContext>();
    }
}