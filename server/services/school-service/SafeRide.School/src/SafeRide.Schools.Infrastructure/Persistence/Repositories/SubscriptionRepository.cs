using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository(SchoolDbContext context)
    : GenericRepository<Subscription>(context),
        ISubscriptionRepository
{
    private static readonly SubscriptionStatus[] Serviceable =
    [
        SubscriptionStatus.Active,
        SubscriptionStatus.InGrace,
    ];

    public Task<Subscription?> GetCurrentForSchoolAsync(
        Guid schoolId,
        CancellationToken ct = default
    ) =>
        Set.Where(s => s.SchoolId == schoolId && Serviceable.Contains(s.Status))
            .OrderByDescending(s => s.EndsOn)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<SubscriptionWithSchool>, int)> SearchAsync(
        SubscriptionStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = Set.AsQueryable();

        if (status is not null)
            query = query.Where(s => s.Status == status);

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

    public async Task<IReadOnlyList<Subscription>> GetServiceableAsync(
        CancellationToken ct = default
    ) => await Set.Where(s => Serviceable.Contains(s.Status)).ToListAsync(ct);

    public async Task<IReadOnlyList<Subscription>> GetEndingOnOrAfterAsync(
        DateOnly date,
        CancellationToken ct = default
    ) =>
        await Set.Where(s => s.Status == SubscriptionStatus.Active && s.EndsOn >= date)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Subscription>> GetEndedBeforeAsync(
        DateOnly date,
        CancellationToken ct = default
    ) =>
        await Set.Where(s => s.Status != SubscriptionStatus.Cancelled && s.EndsOn < date)
            .ToListAsync(ct);
}
