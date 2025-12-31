namespace ShitpostBot.Application.Core;

public interface IMessageProcessor
{
    Task ProcessCreatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default);
    Task ProcessUpdatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default);
    Task ProcessDeletedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default);
}