using Pgvector;
using Pgvector.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure.Extensions;

public static class ConversationFragmentQueryExtensions
{
    extension(IQueryable<ConversationFragment> query)
    {
        public IQueryable<ClosestToConversationFragment> ConversationFragmentsWithClosestEmbedding(
            Vector embedding
        )
        {
            return query
                .OrderBy(fragment => fragment.Embedding.CosineDistance(embedding))
                .ThenBy(fragment => fragment.StartedAt)
                .Select(fragment => new ClosestToConversationFragment(
                    fragment.Id,
                    fragment.GuildId,
                    fragment.ChannelId,
                    fragment.FirstMessageId,
                    fragment.LastMessageId,
                    fragment.StartedAt,
                    fragment.EndedAt,
                    fragment.MessageCount,
                    fragment.Embedding.CosineDistance(embedding)
                ));
        }
    }
}
