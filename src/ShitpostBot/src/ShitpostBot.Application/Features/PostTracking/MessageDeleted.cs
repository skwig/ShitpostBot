using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.PostTracking;

public record MessageDeleted(MessageIdentification Identification) : INotification;