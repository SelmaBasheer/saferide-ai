using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Subscriptions.Query;

public sealed class GetSubscriptionsHandler(
    ISubscriptionRepository subscriptions,
    ISchoolRepository schools
)
{
    /// The school admin's own subscription. Resolved through their user id,
    /// the same way "my school" already is — a school admin's token carries
    /// no schoolId, and inventing one from a request body would be the exact
    /// hole every other service avoids.
    public async Task<Result<Subscription>> GetMineAsync(Guid adminUserId, CancellationToken ct)
    {
        var school = await schools.GetByAdminUserIdAsync(adminUserId, ct);

        if (school is null)
            return Result.Failure<Subscription>(SchoolErrors.SchoolNotFound);

        var subscription = await subscriptions.GetCurrentForSchoolAsync(school.Id, ct);

        return subscription is null
            ? Result.Failure<Subscription>(SubscriptionErrors.NotFound)
            : Result.Success(subscription);
    }

    public Task<(IReadOnlyList<SubscriptionWithSchool> Items, int TotalCount)> SearchAsync(
        SubscriptionStatus? status,
        int page,
        int pageSize,
        CancellationToken ct
    ) => subscriptions.SearchAsync(status, page, pageSize, ct);
}
