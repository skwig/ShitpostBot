using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShitpostBot.Infrastructure;

internal class ShitpostBotContextFactory : IDesignTimeDbContextFactory<ShitpostBotDbContext>
{
    public ShitpostBotDbContext CreateDbContext(string[] args)
    {
        const string connString = "Server=localhost,5432;User ID=postgres;Password=mysecretpassword;";
            
        var optionsBuilder = new DbContextOptionsBuilder<ShitpostBotDbContext>();
        optionsBuilder.UseNpgsql(connString, sqlOpts => sqlOpts
            .CommandTimeout((int)TimeSpan.FromMinutes(100).TotalSeconds)
            .UseVector()
        );

        return new ShitpostBotDbContext(optionsBuilder.Options);
    }
}