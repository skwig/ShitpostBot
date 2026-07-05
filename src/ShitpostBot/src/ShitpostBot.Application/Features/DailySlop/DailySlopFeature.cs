using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.DailySlop;

public class DailySlopFeature(
    IEnumerable<IDailySlopDetector> detectors,
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider
) : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage msg, CancellationToken ct)
    {
        foreach (var detector in detectors)
        {
            if (!detector.Matches(msg))
            {
                continue;
            }

            var entry = new DailySlopEntry(
                msg.Id.PosterId,
                detector.GameId,
                msg.PostedOn,
                dateTimeProvider.UtcNow,
                msg.Id.GuildId,
                msg.Id.ChannelId,
                msg.Id.MessageId
            );

            dbContext.DailySlopEntry.Add(entry);
            await unitOfWork.SaveChangesAsync(ct);

            return true;
        }

        return false;
    }
}
