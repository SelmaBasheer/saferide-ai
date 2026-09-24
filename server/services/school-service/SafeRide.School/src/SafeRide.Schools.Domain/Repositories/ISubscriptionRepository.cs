using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Domain.Repositories;

/// A subscription row with its school's name, for the super admin list. Lives
/// beside the interface because it is the shape of a query result, not a thing
/// the domain reasons about.
public sealed record SubscriptionWithSchool(Subscription Subscription, string SchoolName);

public interface ISubscriptionRepository : IGenericRepository<Subscription>
{
    Task<Subscription?> GetCurrentForSchoolAsync(Guid schoolId, CancellationToken ct = default);

    Task<(IReadOnlyList<SubscriptionWithSchool> Items, int TotalCount)> SearchAsync(
        SubscriptionStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    /// Candidates for a warning: still running, end date not yet passed.
    Task<IReadOnlyList<Subscription>> GetEndingOnOrAfterAsync(
        DateOnly date,
        CancellationToken ct = default
    );

    /// Candidates for suspension: not cancelled, already past their last day of
    /// service. Whether the grace period has also run out is date arithmetic
    /// the entity does.
    Task<IReadOnlyList<Subscription>> GetEndedBeforeAsync(
        DateOnly date,
        CancellationToken ct = default
    );
}
