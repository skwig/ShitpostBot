using System.Threading.Tasks;

namespace ShitpostBot.Domain;

public interface IWhitelistedPostsRepository : IRepository<WhitelistedPost>
{
    Task<WhitelistedPost?> GetByPostId(long postId);
}