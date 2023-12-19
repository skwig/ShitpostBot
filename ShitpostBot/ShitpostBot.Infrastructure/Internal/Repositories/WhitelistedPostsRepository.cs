using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class WhitelistedPostsRepository(ShitpostBotDbContext context) : Repository<WhitelistedPost>(context), IWhitelistedPostsRepository
{
    public Task<WhitelistedPost?> GetByPostId(long postId)
    {
        return Context.WhitelistedPost.SingleOrDefaultAsync(wp => wp.Post.Id == postId);
    }
}