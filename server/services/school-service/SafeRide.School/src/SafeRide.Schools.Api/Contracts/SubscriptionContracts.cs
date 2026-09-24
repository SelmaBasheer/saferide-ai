using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Api.Contracts;

public sealed record ActivateSubscriptionRequest(Guid SchoolId, Guid PlanId, DateOnly? StartsOn);

public sealed record SubscriptionDto(
    Guid Id,
    Guid SchoolId,
    string? SchoolName,
    string PlanName,
    long PriceInPaise,
    int? BusLimit,
    DateOnly StartsOn,
    DateOnly EndsOn,
    DateOnly GraceEndsOn,
    SubscriptionStatus Status,
    int DaysRemaining
);
