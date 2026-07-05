using FastEndpoints;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostMessageEndpoint(MessageRouter router, IDateTimeProvider dateTimeProvider)
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
            req.Attachments
                .Select(a => new Attachment(
                    Id: a.Id,
                    Url: new Uri(a.Url!),
                    MediaType: a.MediaType,
                    Width: a.Width,
                    Height: a.Height))
                .ToList(),
            req.Embeds
                .Select(e => new Embed(
                    Url: new Uri(e.Url))
                )
                .ToList(),
            req.Timestamp ?? dateTimeProvider.UtcNow
        );

        await router.RouteCreate(msg, ct);

        await Send.OkAsync(new PostMessageResponse
        {
            MessageId = req.MessageId,
            Tracked = true
        }, ct);
    }
}