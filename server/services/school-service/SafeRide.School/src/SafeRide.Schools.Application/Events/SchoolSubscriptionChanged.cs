namespace SafeRide.Schools.Application.Events;

public sealed record SchoolSubscriptionChanged(
    Guid SchoolId,
    string Status,
    int? BusLimit,
    DateOnly EndsOn,
    DateTime OccurredAtUtc
);
