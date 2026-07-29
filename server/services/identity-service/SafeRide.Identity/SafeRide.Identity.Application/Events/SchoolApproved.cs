namespace SafeRide.Identity.Application.Events;

public sealed record SchoolApproved(Guid SchoolId, Guid AdminUserId, DateTime OccurredAtUtc);

public sealed record SchoolSuspended(Guid SchoolId, Guid AdminUserId, DateTime OccurredAtUtc);
