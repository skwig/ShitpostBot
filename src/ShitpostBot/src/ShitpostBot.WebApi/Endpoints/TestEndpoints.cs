using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;
using System.Diagnostics;

namespace ShitpostBot.WebApi.Endpoints;

public static class TestEndpoints
{
    public static void MapTestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/test")
            .WithTags("Test");

        group.MapPost("/image-message", PostImageMessage);
        group.MapPost("/link-message", PostLinkMessage);
        group.MapGet("/actions/{messageId}", GetActions);
    }

    private static async Task<IResult> PostImageMessage(
        [FromBody] PostImageMessageRequest request,
        [FromServices] TestMessageFactory factory,
        [FromServices] IMediator mediator)
    {
        var imageMessage = factory.CreateImageMessage(
            request.ImageUrl,
            request.GuildId,
            request.ChannelId,
            request.UserId,
            request.MessageId,
            request.Timestamp
        );

        await mediator.Publish(new ImageMessageCreated(imageMessage));

        return Results.Ok(new PostMessageResponse
        {
            MessageId = imageMessage.Identification.MessageId,
            Tracked = true
        });
    }

    private static async Task<IResult> PostLinkMessage(
        [FromBody] PostLinkMessageRequest request,
        [FromServices] TestMessageFactory factory,
        [FromServices] IMediator mediator)
    {
        var linkMessage = factory.CreateLinkMessage(
            request.LinkUrl,
            request.GuildId,
            request.ChannelId,
            request.UserId,
            request.MessageId,
            request.Timestamp
        );

        await mediator.Publish(new LinkMessageCreated(linkMessage));

        return Results.Ok(new PostMessageResponse
        {
            MessageId = linkMessage.Identification.MessageId,
            Tracked = true
        });
    }

    private static async Task<IResult> GetActions(
        ulong messageId,
        [FromServices] ITestActionLogger logger,
        [FromQuery] int expectedCount = 0,
        [FromQuery] int timeout = 10000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var actions = await logger.WaitForActionsAsync(
            messageId, 
            expectedCount, 
            TimeSpan.FromMilliseconds(timeout)
        );
        
        return Results.Ok(new
        {
            messageId,
            actions,
            waitedMs = stopwatch.ElapsedMilliseconds
        });
    }


}

public record PostImageMessageRequest
{
    public required string ImageUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record PostLinkMessageRequest
{
    public required string LinkUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record PostMessageResponse
{
    public required ulong MessageId { get; init; }
    public required bool Tracked { get; init; }
}