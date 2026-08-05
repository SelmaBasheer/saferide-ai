namespace SafeRide.Identity.Application.Events;

public sealed record SchoolCreated(Guid SchoolId, Guid AdminUserId, DateTime OccurredAtUtc);
