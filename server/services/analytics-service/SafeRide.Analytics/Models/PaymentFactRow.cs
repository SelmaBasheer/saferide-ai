namespace SafeRide.Analytics.Models;

public sealed record PaymentFactRow(
    Guid PaymentId,
    Guid SchoolId,
    Guid? SubscriptionId,
    string? PlanName,
    long AmountPaise,
    string Status,
    DateTime CapturedAtUtc,
    DateOnly PaymentDate
);
