using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository(SchoolDbContext context)
    : GenericRepository<Subscription>(context),
        ISubscriptionRepository
{
    public Task<Subscription?> GetCurrentForSchoolAsync(
        Guid schoolId,
        CancellationToken ct = default
    )
    {
        var graceCutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-Subscription.GraceDays);

        return Set.Where(s =>
                s.SchoolId == schoolId
                && s.Status != SubscriptionStatus.Cancelled
                && s.EndsOn >= graceCutoff
            )
            .OrderByDescending(s => s.EndsOn)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(IReadOnlyList<SubscriptionWithSchool>, int)> SearchAsync(
        SubscriptionStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var graceCutoff = today.AddDays(-Subscription.GraceDays);

        // Only Active and Cancelled are ever stored. The other two are date
        // arithmetic, so each effective status becomes a condition on EndsOn.
        // Doing this in SQL rather than after the fact keeps totalCount honest.
        var query = status switch
        {
            SubscriptionStatus.Cancelled => Set.Where(s =>
                s.Status == SubscriptionStatus.Cancelled
            ),

            SubscriptionStatus.Active => Set.Where(s =>
                s.Status != SubscriptionStatus.Cancelled && s.EndsOn >= today
            ),

            SubscriptionStatus.InGrace => Set.Where(s =>
                s.Status != SubscriptionStatus.Cancelled
                && s.EndsOn < today
                && s.EndsOn >= graceCutoff
            ),

            SubscriptionStatus.Expired => Set.Where(s =>
                s.Status != SubscriptionStatus.Cancelled && s.EndsOn < graceCutoff
            ),

            _ => Set.AsQueryable(),
        };

        var total = await query.CountAsync(ct);

        // Joined in SQL rather than fetching subscriptions and then looking up
        // each school — that would be one query per row.
        var items = await query
            .OrderByDescending(s => s.EndsOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                context.Schools,
                s => s.SchoolId,
                school => school.Id,
                (s, school) => new SubscriptionWithSchool(s, school.Name)
            )
            .ToListAsync(ct);

        return (items, total);
    }

    /// Candidates for a warning: still running, end date not yet passed.
    public async Task<IReadOnlyList<Subscription>> GetEndingOnOrAfterAsync(
        DateOnly date,
        CancellationToken ct = default
    ) =>
        await Set.Where(s => s.Status == SubscriptionStatus.Active && s.EndsOn >= date)
            .ToListAsync(ct);

    /// Candidates for suspension: not cancelled, already past their last day of
    /// service. Whether the grace period has also run out is date arithmetic
    /// the entity does.
    public async Task<IReadOnlyList<Subscription>> GetEndedBeforeAsync(
        DateOnly date,
        CancellationToken ct = default
    ) =>
        await Set.Where(s => s.Status != SubscriptionStatus.Cancelled && s.EndsOn < date)
            .ToListAsync(ct);
}
