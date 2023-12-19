using System;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public record TextMessage(MessageIdentification Identification, string? Content, DateTimeOffset PostedOn);