namespace DriverService.Messaging;

public sealed record SchoolApproved(Guid SchoolId);

public sealed record SchoolSuspended(Guid SchoolId);
