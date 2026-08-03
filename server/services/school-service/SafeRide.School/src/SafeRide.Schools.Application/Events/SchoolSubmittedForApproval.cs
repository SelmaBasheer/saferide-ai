namespace SafeRide.Schools.Application.Events;

public sealed record SchoolSubmittedForApproval(
    Guid SchoolId,
    string SchoolName,
    string AdminEmail,
    DateTime SubmittedAtUtc
);
