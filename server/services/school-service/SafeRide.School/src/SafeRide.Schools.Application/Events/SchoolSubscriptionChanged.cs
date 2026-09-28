namespace SafeRide.Schools.Application.Events;

// PlanName added last with a default, so ExpireSubscriptionsHandler and any
// other publisher compile unchanged. A null never erases a plan name Analytics
// already holds.
public sealed record SchoolSubscriptionChanged(
    Guid SchoolId,
    string Status,
    int? BusLimit,
    DateOnly EndsOn,
    DateTime OccurredAtUtc,
    string? PlanName = null
);
