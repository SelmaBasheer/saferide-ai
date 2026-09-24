using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Subscriptions.Command;

/// <summary>
/// Two jobs on one pass: warn schools whose subscription is about to end, and
/// suspend the ones whose grace period has gone.
///
/// The suspension writes no new enforcement. Suspending a school publishes
/// school-suspended, and Route, Bus and the rest already refuse to work for a
/// suspended school — so the entire consequence of not paying is one status
/// change on one row.
///
/// Safe to run as often as you like. A warning already sent today is skipped,
/// and a school already suspended is skipped.
/// </summary>
public sealed class ExpireSubscriptionsHandler(
    ISubscriptionRepository subscriptions,
    IGenericRepository<School> schools,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher
)
{
    public sealed record Outcome(int Warned, IReadOnlyList<Guid> Suspended);

    public async Task<Outcome> RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var warnings = await WarnAsync(today, ct);
        var suspended = await SuspendAsync(today, ct);

        return new Outcome(warnings, suspended);
    }

    private async Task<int> WarnAsync(DateOnly today, CancellationToken ct)
    {
        var candidates = await subscriptions.GetEndingOnOrAfterAsync(today, ct);

        var due = candidates.Where(s => s.ShouldWarn(today)).ToList();

        if (due.Count == 0)
            return 0;

        var events = new List<SubscriptionExpiring>();

        foreach (var subscription in due)
        {
            var school = await schools.GetByIdAsync(subscription.SchoolId, ct);

            if (school is null)
                continue;

            events.Add(
                new SubscriptionExpiring(
                    school.Id,
                    school.Name,
                    school.AdminEmail,
                    subscription.PlanName,
                    subscription.EndsOn,
                    subscription.DaysRemaining(today),
                    DateTime.UtcNow
                )
            );

            subscription.MarkWarningSent(today);
        }

        // Marked as sent before publishing, on purpose. If the broker is down
        // the warning is lost rather than repeated — and a warning that arrives
        // twenty-four times is worse than one that arrives once on the next
        // warning day.
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var e in events)
        {
            await publisher.PublishAsync(
                MessagingConstants.SchoolEventsExchange,
                MessagingConstants.SubscriptionExpiringKey,
                e,
                ct
            );
        }

        return events.Count;
    }

    private async Task<IReadOnlyList<Guid>> SuspendAsync(DateOnly today, CancellationToken ct)
    {
        var ended = await subscriptions.GetEndedBeforeAsync(today, ct);

        var suspended = new List<School>();

        foreach (var subscription in ended)
        {
            // Past the end date is not enough — the grace period has to have
            // run out too.
            if (subscription.EffectiveStatus(today) != SubscriptionStatus.Expired)
                continue;

            var school = await schools.GetByIdAsync(subscription.SchoolId, ct);

            // Already suspended, rejected, or never approved. Suspend() only
            // accepts an approved school and would throw otherwise.
            if (school is null || school.Status != SchoolStatus.Approved)
                continue;

            school.Suspend();
            suspended.Add(school);
        }

        if (suspended.Count == 0)
            return [];

        // One save for the batch, then announce. Publishing before the save
        // would tell five services about a suspension that might not survive
        // the transaction.
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var school in suspended)
        {
            await publisher.PublishAsync(
                MessagingConstants.SchoolEventsExchange,
                MessagingConstants.SchoolSuspendedKey,
                new SchoolSuspended(school.Id, school.AdminUserId, DateTime.UtcNow),
                ct
            );
        }

        return [.. suspended.Select(s => s.Id)];
    }
}
