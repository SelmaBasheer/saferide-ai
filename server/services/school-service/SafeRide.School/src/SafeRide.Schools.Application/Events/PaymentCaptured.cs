namespace SafeRide.Schools.Application.Events;

/// Every rupee that actually arrived, whether or not it bought a subscription.
/// PlanName and SubscriptionId are null on the paths where the money was taken
/// but nothing was sold — those are precisely the ones somebody has to refund,
/// so they must appear in the revenue report rather than vanish from it.
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
