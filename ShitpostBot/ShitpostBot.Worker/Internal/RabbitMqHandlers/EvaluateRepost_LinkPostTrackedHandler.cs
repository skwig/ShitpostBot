using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;
using NServiceBus;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Worker;

internal class EvaluateRepost_LinkPostTrackedHandler(
    ILogger<EvaluateRepost_LinkPostTrackedHandler> logger,
    IUnitOfWork unitOfWork,
    IOptions<RepostServiceOptions> options,
    IChatClient chatClient)
    : IHandleMessages<LinkPostTracked>
{
    private readonly string[] repostReactions =
    {
        ":police_car:",
        // ":regional_indicator_r:",
        // ":regional_indicator_e:",
        // ":regional_indicator_p:",
        // ":regional_indicator_o:",
        // ":regional_indicator_s:",
        // ":regional_indicator_t:",
        ":rotating_light:"
    };

    public async Task Handle(LinkPostTracked message, IMessageHandlerContext context)
    {
        var postToBeEvaluated = await unitOfWork.LinkPostsRepository.GetById(message.LinkPostId);
        if (postToBeEvaluated == null)
        {
            // TODO: handle
            throw new NotImplementedException();
        }

        // // TODO: move to a different handler
        // if (postToBeEvaluated.Statistics?.MostSimilarTo != null &&
        //     postToBeEvaluated.Statistics.MostSimilarTo.Similarity >= options.Value.RepostSimilarityThreshold)
        // {
        //     var identification = new MessageIdentification(
        //         postToBeEvaluated.ChatGuildId,
        //         postToBeEvaluated.ChatChannelId,
        //         postToBeEvaluated.PosterId,
        //         postToBeEvaluated.ChatMessageId
        //     );
        //
        //     foreach (var repostReaction in repostReactions)
        //     {
        //         await chatClient.React(identification, repostReaction);
        //         await Task.Delay(TimeSpan.FromMilliseconds(500), context.CancellationToken);
        //     }
        // }
    }
}