using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Worker;

internal class EvaluateRepost_ImagePostTrackedHandler(
    ILogger<EvaluateRepost_ImagePostTrackedHandler> logger,
    IImageFeatureExtractorApi imageFeatureExtractorApi,
    IUnitOfWork unitOfWork,
    IOptions<RepostServiceOptions> options,
    IChatClient chatClient,
    IDateTimeProvider dateTimeProvider,
    IImagePostsReader imagePostsReader)
    : IHandleMessages<ImagePostTracked>
{
    private readonly ILogger<EvaluateRepost_ImagePostTrackedHandler> logger = logger;

    private readonly IChatClient chatClient = chatClient;
    private readonly IOptions<RepostServiceOptions> options = options;
    private readonly IImagePostsReader imagePostsReader = imagePostsReader;

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

    public async Task Handle(ImagePostTracked message, IMessageHandlerContext context)
    {
        var postToBeEvaluated = await unitOfWork.ImagePostsRepository.GetById(message.ImagePostId);
        if (postToBeEvaluated == null)
        {
            // TODO: handle
            throw new NotImplementedException();
        }

        var imageFeatures = await imageFeatureExtractorApi.ExtractImageFeaturesAsync(postToBeEvaluated.Image.ImageUri.ToString());

        postToBeEvaluated.SetImageFeatures(new ImageFeatures(new Vector(imageFeatures.ImageFeatures)), dateTimeProvider.UtcNow);
            
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        // imagePostsReader.FromSql("SELECT * FROM HOVNO");
        //
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
        //         await Task.Delay(TimeSpan.FromMilliseconds(500));
        //     }
        // }
    }

    private static (TResult, Stopwatch) BenchmarkedExecute<TResult>(Func<TResult> func)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var result = func();

        stopwatch.Stop();

        return (result, stopwatch);
    }
}