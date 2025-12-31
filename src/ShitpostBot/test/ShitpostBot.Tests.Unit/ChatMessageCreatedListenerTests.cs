using FluentAssertions;
using NSubstitute;
using ShitpostBot.Application.Core;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ChatMessageCreatedListenerTests
{
    [Fact]
    public void Constructor_AcceptsMessageProcessor()
    {
        // Arrange
        var messageProcessor = Substitute.For<IMessageProcessor>();

        // Act
        var listener = new ChatMessageCreatedListener(messageProcessor);

        // Assert
        listener.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_DoesNotAcceptOldDependencies()
    {
        // This test verifies we've refactored away from IChatClient and IMediator
        // The listener should only depend on IMessageProcessor

        var constructors = typeof(ChatMessageCreatedListener).GetConstructors();
        constructors.Should().HaveCount(1);

        var constructor = constructors[0];
        var parameters = constructor.GetParameters();

        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(IMessageProcessor));
    }
}
