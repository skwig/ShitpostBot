using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ShitpostBot.Infrastructure
{
    internal class DbContextFactory<TDbContext> : IDbContextFactory<TDbContext> where TDbContext : DbContext
    {
        private readonly IServiceProvider serviceProvider;

        public DbContextFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public TDbContext CreateDbContext()
        {
            return serviceProvider.GetRequiredService<TDbContext>();
        }
    }
}