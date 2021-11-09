using System;
using System.Linq;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure
{
    public interface IImagePostsReader : IReader<ImagePost>
    {
    }

    internal class ImagePostsReader : Reader<ImagePost>, IImagePostsReader
    {
        public ImagePostsReader(IDbContextFactory<ShitpostBotDbContext> contextFactory) : base(contextFactory)
        {
        }
    }
}