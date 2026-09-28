namespace SafeRide.Analytics.Messaging.Events;

/// Published by the School service. Its contract, mirrored — not redesigned.
public sealed record SchoolApproved(
    Guid SchoolId,
    Guid AdminUserId,
    string SchoolName,
    string AdminEmail,
    string City,
    DateTime OccurredAtUtc
);

/// Carries no name, which is why suspension can never create a row.
public sealed record SchoolSuspended(Guid SchoolId, Guid AdminUserId, DateTime OccurredAtUtc);

public sealed record SchoolSubscriptionChanged(
    Guid SchoolId,
    string Status,
    int? BusLimit,
    DateOnly EndsOn,
    DateTime OccurredAtUtc,
    string? PlanName
);

public sealed record PaymentCaptured(
    Guid PaymentId,
    Guid SchoolId,
    Guid? SubscriptionId,
    string? PlanName,
    long AmountInPaise,
    string Status,
    DateTime CapturedAtUtc,
    DateTime OccurredAtUtc
);
