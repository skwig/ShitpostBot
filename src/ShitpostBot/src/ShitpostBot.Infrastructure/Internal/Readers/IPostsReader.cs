using ShitpostBot.Domain;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure;

public interface IPostsReader : IReader<Post>;

internal class PostsReader(IDbContextFactory<ShitpostBotDbContext> contextFactory) : Reader<Post>(contextFactory), IPostsReader;