namespace SafeRide.Schools.Application.Events;

/// <summary>
/// Sent 7, 3 and 1 days before a subscription ends.
///
/// Carries the recipient's address, like every other event the Notification
/// service consumes — that service holds no database and cannot look anyone up.
/// </summary>
public sealed record SubscriptionExpiring(
    Guid SchoolId,
    string SchoolName,
    string AdminEmail,
    string PlanName,
    DateOnly EndsOn,
    int DaysRemaining,
    DateTime OccurredAtUtc
);
