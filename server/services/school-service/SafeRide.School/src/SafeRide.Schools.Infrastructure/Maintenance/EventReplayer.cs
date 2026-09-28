using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Infrastructure.Persistence;

namespace SafeRide.Schools.Infrastructure.Maintenance;

public sealed class EventReplayer(
    SchoolDbContext db,
    IEventPublisher publisher,
    ILogger<EventReplayer> logger
) : IEventReplayer
{
    public async Task<ReplaySummary> ReplayAsync(CancellationToken ct)
    {
        var schools = await db.Schools.AsNoTracking().ToListAsync(ct);

        var approved = 0;
        var suspended = 0;

        foreach (var school in schools)
        {
            // The entity's own timestamp, never DateTime.UtcNow. A consumer's
            // ordering guard compares against what it already holds — stamping
            // "now" would let a replay overwrite a newer live event.
            switch (school.Status)
            {
                case SchoolStatus.Approved:
                    await publisher.PublishAsync(
                        MessagingConstants.SchoolEventsExchange,
                        MessagingConstants.SchoolApprovedKey,
                        new SchoolApproved(
                            school.Id,
                            school.AdminUserId,
                            school.Name,
                            school.AdminEmail,
                            school.City,
                            school.UpdatedAtUtc
                        ),
                        ct
                    );
                    approved++;
                    break;

                case SchoolStatus.Suspended:
                    await publisher.PublishAsync(
                        MessagingConstants.SchoolEventsExchange,
                        MessagingConstants.SchoolSuspendedKey,
                        new SchoolSuspended(school.Id, school.AdminUserId, school.UpdatedAtUtc),
                        ct
                    );
                    suspended++;
                    break;

                // Draft, pending and rejected schools are deliberately skipped.
                // Nothing downstream has a use for a school that was never live.
            }
        }

        // Oldest first, so a school with a history of subscriptions ends on its
        // current one rather than whichever happened to be read last.
        var subscriptions = await db
            .Subscriptions.AsNoTracking()
            .OrderBy(s => s.UpdatedAtUtc)
            .ToListAsync(ct);

        foreach (var subscription in subscriptions)
        {
            await publisher.PublishAsync(
                MessagingConstants.SchoolEventsExchange,
                MessagingConstants.SchoolSubscriptionChangedKey,
                new SchoolSubscriptionChanged(
                    subscription.SchoolId,
                    subscription.Status.ToString(),
                    subscription.BusLimit,
                    subscription.EndsOn,
                    subscription.UpdatedAtUtc,
                    subscription.PlanName
                ),
                ct
            );
        }

        // The plan name lives on the plan, not the payment. One dictionary
        // rather than a query per payment.
        var planNames = await db
            .SubscriptionPlans.AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var payments = await db
            .Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Captured)
            .ToListAsync(ct);

        foreach (var payment in payments)
        {
            await publisher.PublishAsync(
                MessagingConstants.SchoolEventsExchange,
                MessagingConstants.PaymentCapturedKey,
                new PaymentCaptured(
                    payment.Id,
                    payment.SchoolId,
                    payment.SubscriptionId,
                    planNames.GetValueOrDefault(payment.PlanId),
                    payment.AmountInPaise,
                    payment.Status.ToString(),
                    payment.CapturedAtUtc ?? payment.UpdatedAtUtc,
                    payment.UpdatedAtUtc
                ),
                ct
            );
        }

        var summary = new ReplaySummary(approved, suspended, subscriptions.Count, payments.Count);

        // Warning, not Information. A human deliberately replaying history is
        // worth being able to find in the logs afterwards.
        logger.LogWarning("Replayed events: {@Summary}", summary);

        return summary;
    }
}
