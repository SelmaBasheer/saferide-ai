using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Plans.Query;

public sealed class GetPlansHandler(ISubscriptionPlanRepository plans)
{
    public Task<IReadOnlyList<SubscriptionPlan>> GetAsync(
        bool includeInactive,
        CancellationToken ct
    ) => plans.ListAsync(includeInactive, ct);
}
