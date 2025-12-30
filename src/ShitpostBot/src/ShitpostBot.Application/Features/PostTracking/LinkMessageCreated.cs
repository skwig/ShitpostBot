using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.PostTracking;

public record LinkMessageCreated(LinkMessage LinkMessage) : INotification;