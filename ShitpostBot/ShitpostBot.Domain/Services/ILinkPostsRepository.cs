using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShitpostBot.Domain
{
    public interface ILinkPostsRepository : IRepository<LinkPost>
    {
        Task<LinkPost> GetById(long id);
        Task<IReadOnlyList<LinkPost>> GetHistory(DateTimeOffset postedAtFromInclusive, DateTimeOffset postedAtToExclusive);
    }
}