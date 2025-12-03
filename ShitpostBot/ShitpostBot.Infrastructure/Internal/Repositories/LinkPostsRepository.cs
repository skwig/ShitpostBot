using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class LinkPostsRepository(ShitpostBotDbContext context) : Repository<LinkPost>(context), ILinkPostsRepository
{
    public Task<LinkPost?> GetById(long id)
    {
        return Context.LinkPost.SingleOrDefaultAsync(ip => ip.Id == id);
    }

    public async Task<IReadOnlyList<LinkPost>> GetHistory(DateTimeOffset postedAtFromInclusive, DateTimeOffset postedAtToExclusive)
    {
        var posts = await Context.LinkPost
            .AsNoTracking()
            .Where(x => postedAtFromInclusive <= x.PostedOn && x.PostedOn < postedAtToExclusive)
            .ToListAsync();

        return posts;
    }
}