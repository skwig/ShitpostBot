using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShitpostBot.Domain;

public interface IImagePostsRepository : IRepository<ImagePost>
{
    Task<ImagePost> GetById(long id);
    Task<IReadOnlyList<ImagePost>> GetHistory(DateTimeOffset postedAtFromInclusive, DateTimeOffset postedAtToExclusive);
}