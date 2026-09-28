namespace SafeRide.Schools.Application.Events;

public sealed record SchoolApproved(
    Guid SchoolId,
    Guid AdminUserId,
    string SchoolName,
    string AdminEmail,
    string City,
    DateTime OccurredAtUtc
);
