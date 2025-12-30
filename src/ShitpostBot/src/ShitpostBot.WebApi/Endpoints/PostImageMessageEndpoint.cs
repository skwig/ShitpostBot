using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostImageMessageEndpoint : Endpoint<PostImageMessageRequest, PostMessageResponse>
{
    private readonly TestMessageFactory _factory;
    private readonly IMediator _mediator;

    public PostImageMessageEndpoint(TestMessageFactory factory, IMediator mediator)
    {
        _factory = factory;
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/test/image-message");
        AllowAnonymous();
        Tags("Test");
    }

    public override async Task HandleAsync(PostImageMessageRequest req, CancellationToken ct)
    {
        var imageMessage = _factory.CreateImageMessage(
            req.ImageUrl,
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId,
            req.Timestamp
        );

        await _mediator.Publish(new ImageMessageCreated(imageMessage), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = imageMessage.Identification.MessageId,
            Tracked = true
        }, ct);
    }
}
