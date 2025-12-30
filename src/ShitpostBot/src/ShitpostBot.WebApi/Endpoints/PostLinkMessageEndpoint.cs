using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostLinkMessageEndpoint : Endpoint<PostLinkMessageRequest, PostMessageResponse>
{
    private readonly TestMessageFactory _factory;
    private readonly IMediator _mediator;

    public PostLinkMessageEndpoint(TestMessageFactory factory, IMediator mediator)
    {
        _factory = factory;
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/test/link-message");
        AllowAnonymous();
        Tags("Test");
    }

    public override async Task HandleAsync(PostLinkMessageRequest req, CancellationToken ct)
    {
        var linkMessage = _factory.CreateLinkMessage(
            req.LinkUrl,
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId,
            req.Timestamp
        );

        await _mediator.Publish(new LinkMessageCreated(linkMessage), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = linkMessage.Identification.MessageId,
            Tracked = true
        }, ct);
    }
}
