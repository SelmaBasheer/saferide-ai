namespace DriverService.Messaging;

public sealed record SchoolApproved(Guid SchoolId, DateTime OccurredAtUtc);

public sealed record SchoolSuspended(Guid SchoolId, DateTime OccurredAtUtc);
