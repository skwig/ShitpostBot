using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Infrastructure;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostBotCommandEndpoint : Endpoint<PostBotCommandRequest, PostMessageResponse>
{
    private readonly TestMessageFactory _factory;
    private readonly IMediator _mediator;

    public PostBotCommandEndpoint(TestMessageFactory factory, IMediator mediator)
    {
        _factory = factory;
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/test/bot-command");
        AllowAnonymous();
        Options(x => x.WithTags("Test"));
    }

    public override async Task HandleAsync(PostBotCommandRequest req, CancellationToken ct)
    {
        var commandMessageIdentification = _factory.GenerateMessageIdentification(
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId
        );

        MessageIdentification? referencedMessageIdentification = null;
        if (req.ReferencedMessageId.HasValue)
        {
            referencedMessageIdentification = _factory.GenerateMessageIdentification(
                req.GuildId,
                req.ChannelId,
                req.ReferencedUserId,
                req.ReferencedMessageId
            );
        }

        await _mediator.Send(new ExecuteBotCommand(
            commandMessageIdentification,
            referencedMessageIdentification,
            new BotCommand(req.Command)
        ), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = commandMessageIdentification.MessageId,
            Tracked = true
        }, ct);
    }
}
