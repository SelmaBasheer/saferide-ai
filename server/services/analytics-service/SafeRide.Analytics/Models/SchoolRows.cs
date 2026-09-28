namespace SafeRide.Analytics.Models;

/// From school-approved and school-suspended. Name is null on suspension,
/// so the write has to treat null as "unchanged".
public sealed record SchoolStatusRow(
    Guid SchoolId,
    string? Name,
    string? City,
    string Status,
    DateTime EventAtUtc
);

/// From school-subscription-changed — the other, independent stream.
public sealed record SchoolEntitlementRow(
    Guid SchoolId,
    string? PlanName,
    string SubscriptionStatus,
    DateOnly SubscriptionEndsOn,
    int? BusLimit,
    DateTime EventAtUtc
);
