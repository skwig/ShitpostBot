using System;
using System.Linq;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public interface IPostsReader : IReader<Post>
{
}

internal class PostsReader(IDbContextFactory<ShitpostBotDbContext> contextFactory) : Reader<Post>(contextFactory), IPostsReader;