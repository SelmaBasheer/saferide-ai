using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Infrastructure.Persistence.Repositories;

public class SubscriptionPlanRepository(SchoolDbContext context)
    : GenericRepository<SubscriptionPlan>(context),
        ISubscriptionPlanRepository
{
    public async Task<IReadOnlyList<SubscriptionPlan>> ListAsync(
        bool includeInactive,
        CancellationToken ct = default
    )
    {
        var query = Set.AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        // Cheapest first, so the list reads like a pricing page.
        return await query.OrderBy(p => p.PriceInPaise).ToListAsync(ct);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken ct = default) =>
        Set.AnyAsync(p => p.Name == name, ct);
}
