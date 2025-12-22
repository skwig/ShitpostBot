using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public interface IDbContext
{
    DbSet<Post> Post { get; }
    DbSet<ImagePost> ImagePost { get; }
    DbSet<LinkPost> LinkPost { get; }
    DbSet<WhitelistedPost> WhitelistedPost { get; }
}