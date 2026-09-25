using SafeRide.Schools.Domain.Entities;

namespace SafeRide.Schools.Domain.Repositories;

public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan>
{
    Task<IReadOnlyList<SubscriptionPlan>> ListAsync(
        bool includeInactive,
        CancellationToken ct = default
    );

    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
