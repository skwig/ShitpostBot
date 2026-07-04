using FastEndpoints;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Endpoints;

public class PostMessageEndpoint(MessageRouter router)
    : Endpoint<PostMessageRequest, PostMessageResponse>
{
    public override void Configure()
    {
        Post("/test/messages");
        Tags("Test");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PostMessageRequest req, CancellationToken ct)
    {
        var messageId = req.MessageId ?? 0;
        var channelId = req.ChannelId ?? 0;
        var guildId = req.GuildId ?? 0;
        var userId = req.UserId ?? 0;

        MessageIdentification? repliedTo = null;
        if (req.RepliedToMessageId.HasValue)
        {
            repliedTo = new MessageIdentification(
                guildId,
                channelId,
                req.RepliedToUserId ?? 0,
                req.RepliedToMessageId.Value
            );
        }

        var msg = new IncomingMessage(
            new MessageIdentification(guildId, channelId, userId, messageId),
            repliedTo,
            req.Content,
            req.Attachments?.Where(a => a.Url != null).Select(a => new Attachment(a.Id, new Uri(a.Url!), a.MediaType)).ToList() ?? [],
            req.Embeds?.Where(e => e.Url != null).Select(e => new Embed(new Uri(e.Url!))).ToList() ?? [],
            req.Timestamp ?? DateTimeOffset.UtcNow
        );

        await router.RouteCreate(msg);

        await Send.OkAsync(new PostMessageResponse
        {
            MessageId = messageId,
            Tracked = true
        }, ct);
    }
}