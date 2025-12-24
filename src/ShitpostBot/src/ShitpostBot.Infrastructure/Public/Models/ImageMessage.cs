using System;

namespace ShitpostBot.Infrastructure;

public record ImageMessage(MessageIdentification Identification, ImageMessageAttachment Attachment, DateTimeOffset PostedOn);

public record ImageMessageAttachment(ulong Id, string FileName, Uri Uri, string? MediaType);