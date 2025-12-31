using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ShitpostBot.Application.Core;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Infrastructure.Services;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class MessageProcessorTests
{
    [Fact]
    public async Task ProcessCreatedMessage_WithBotCommand_SendsExecuteBotCommand()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<MessageProcessor>>();
        var chatClient = Substitute.For<IChatClient>();
        var chatUtils = Substitute.For<IChatClientUtils>();
        chatUtils.Mention(Arg.Any<ulong>(), Arg.Any<bool>()).Returns("<@123>");
        chatClient.Utils.Returns(chatUtils);

        var processor = new MessageProcessor(logger, chatClient, mediator);

        var messageData = new MessageData(
            GuildId: 1,
            ChannelId: 2,
            UserId: 3,
            MessageId: 4,
            CurrentMemberId: 123,
            Content: "<@123> about",
            Attachments: [],
            Embeds: [],
            ReferencedMessage: null,
            Timestamp: DateTimeOffset.UtcNow
        );

        // Act
        await processor.ProcessCreatedMessageAsync(messageData);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<ExecuteBotCommand>(cmd =>
                cmd.Command.Command == "about" &&
                cmd.Identification.MessageId == 4),
            Arg.Any<CancellationToken>()
        );
    }
}