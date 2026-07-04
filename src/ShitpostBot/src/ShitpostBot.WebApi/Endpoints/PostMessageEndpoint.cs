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
        MessageIdentification? repliedTo = null;
        if (req.RepliedToMessageId.HasValue)
        {
            repliedTo = new MessageIdentification(
                req.GuildId,
                req.ChannelId,
                req.RepliedToUserId ?? 0,
                req.RepliedToMessageId.Value
            );
        }

        var msg = new IncomingMessage(
            new MessageIdentification(req.GuildId, req.ChannelId, req.UserId, req.MessageId),
            repliedTo,
            req.Content,
            req.Attachments?.Where(a => a.Url != null).Select(a => new Attachment(a.Id, new Uri(a.Url!), a.MediaType, a.Width, a.Height)).ToList() ?? [],
            req.Embeds?.Where(e => e.Url != null).Select(e => new Embed(new Uri(e.Url!))).ToList() ?? [],
            req.Timestamp ?? DateTimeOffset.UtcNow
        );

        await router.RouteCreate(msg, ct);

        await Send.OkAsync(new PostMessageResponse
        {
            MessageId = req.MessageId,
            Tracked = true
        }, ct);
    }
}