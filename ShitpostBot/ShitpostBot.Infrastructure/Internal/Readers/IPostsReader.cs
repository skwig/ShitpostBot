using System;
using System.Linq;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure
{
    public interface IPostsReader : IReader<Post>
    {
    }

    internal class PostsReader : Reader<Post>, IPostsReader
    {
        public PostsReader(IDbContextFactory<ShitpostBotDbContext> contextFactory) : base(contextFactory)
        {
        }
    }
}