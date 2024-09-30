using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class ImagePostsRepository(ShitpostBotDbContext context) : Repository<ImagePost>(context), IImagePostsRepository
{
    public Task<ImagePost?> GetById(long id)
    {
        return Context.ImagePost.SingleOrDefaultAsync(ip => ip.Id == id);
    }

    public async Task<IReadOnlyList<ImagePost>> GetHistory(DateTimeOffset postedAtFromInclusive, DateTimeOffset postedAtToExclusive)
    {
        var posts = await Context.ImagePost
            .AsNoTracking()
            .Where(x => postedAtFromInclusive <= x.PostedOn && x.PostedOn < postedAtToExclusive)
            .ToListAsync();

        return posts;
    }
}