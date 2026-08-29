using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.UpdatedMessages;

public record UpdatedMessage(
    MessageIdentification Id,
    string BeforeContent,
    string AfterContent,
    DateTimeOffset PostedOn,
    DateTimeOffset UpdatedOn
);
