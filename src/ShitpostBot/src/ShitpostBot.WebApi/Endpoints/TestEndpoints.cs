using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public static class TestEndpoints
{
    public static void MapTestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/test")
            .WithTags("Test");

        group.MapPost("/image-message", PostImageMessage);
        group.MapPost("/link-message", PostLinkMessage);
        group.MapGet("/events", StreamEvents);
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

    private static async Task StreamEvents(
        HttpContext context,
        [FromServices] ITestEventPublisher publisher)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        await foreach (var testEvent in publisher.SubscribeAsync(context.RequestAborted))
        {
            await context.Response.WriteAsync($"event: {testEvent.Type}\n");
            await context.Response.WriteAsync($"data: {testEvent.DataJson}\n\n");
            await context.Response.Body.FlushAsync();
        }
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