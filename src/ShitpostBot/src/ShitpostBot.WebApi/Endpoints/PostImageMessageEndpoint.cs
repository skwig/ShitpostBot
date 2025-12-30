using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostImageMessageEndpoint(
    TestMessageFactory factory,
    IMediator mediator)
    : Endpoint<PostImageMessageRequest, PostMessageResponse>
{
    public override void Configure()
    {
        Post("/test/image-message");
        Tags("Test");
    }

    public override async Task HandleAsync(PostImageMessageRequest req, CancellationToken ct)
    {
        var imageMessage = factory.CreateImageMessage(
            req.ImageUrl,
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId,
            req.Timestamp
        );

        await mediator.Publish(new ImageMessageCreated(imageMessage), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = imageMessage.Identification.MessageId,
            Tracked = true
        }, ct);
    }
}