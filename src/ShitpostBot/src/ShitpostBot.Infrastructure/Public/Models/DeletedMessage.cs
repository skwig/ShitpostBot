namespace ShitpostBot.Infrastructure;

public record DeletedMessage(
    MessageIdentification Id,
    string AuthorName,
    string Content,
    DateTimeOffset DeletedOn
);
