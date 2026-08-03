namespace SafeRide.Schools.Application.Events;

public sealed record SchoolSuspended(Guid SchoolId, Guid AdminUserId, DateTime OccurredAtUtc);
