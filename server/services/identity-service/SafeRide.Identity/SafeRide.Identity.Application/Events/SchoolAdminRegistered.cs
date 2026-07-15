namespace SafeRide.Identity.Application.Events;

public sealed record SchoolAdminRegistered(
    Guid UserId,
    string Email,
    string FullName,
    string Phone,
    string SchoolName,
    string SchoolAddress,
    string City,
    string State,
    string Pincode,
    DateTime OccurredAtUtc
);
