namespace ShitpostBot.Infrastructure;

public record DeletedMessage(
    MessageIdentification Id,
    string Content,
    DateTimeOffset PostedOn,
    DateTimeOffset DeletedOn
);
