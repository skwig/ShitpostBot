using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.EditedMessages;

public record EditedMessage(
    MessageIdentification Id,
    string BeforeContent,
    string AfterContent,
    DateTimeOffset PostedOn,
    DateTimeOffset UpdatedOn
);
