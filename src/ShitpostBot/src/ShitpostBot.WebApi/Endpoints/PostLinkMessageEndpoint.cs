using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostLinkMessageEndpoint(
    TestMessageFactory factory,
    IMediator mediator)
    : Endpoint<PostLinkMessageRequest, PostMessageResponse>
{
    public override void Configure()
    {
        Post("/test/link-message");
        Tags("Test");
    }

    public override async Task HandleAsync(PostLinkMessageRequest req, CancellationToken ct)
    {
        var linkMessage = factory.CreateLinkMessage(
            req.LinkUrl,
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId,
            req.Timestamp
        );

        await mediator.Publish(new LinkMessageCreated(linkMessage), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = linkMessage.Identification.MessageId,
            Tracked = true
        }, ct);
    }
}