using Microsoft.EntityFrameworkCore;

namespace ShitpostBot.Infrastructure
{
    internal interface IDbContextFactory<TDbContext> where TDbContext : DbContext
    {
        public TDbContext CreateDbContext();
    }
}