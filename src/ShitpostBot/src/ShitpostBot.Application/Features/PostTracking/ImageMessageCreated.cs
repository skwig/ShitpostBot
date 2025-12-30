using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.PostTracking;

public record ImageMessageCreated(ImageMessage ImageMessage) : INotification;