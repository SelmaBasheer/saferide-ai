namespace SafeRide.Schools.Application.Events;

public sealed record SchoolRejected(
    Guid SchoolId,
    Guid AdminUserId,
    string SchoolName,
    string AdminEmail,
    string Reason,
    DateTime OccurredAtUtc
);
