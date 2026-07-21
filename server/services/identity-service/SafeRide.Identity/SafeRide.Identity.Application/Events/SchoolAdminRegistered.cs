namespace SafeRide.Identity.Application.Events;

public sealed record SchoolAdminRegistered(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Phone,
    string SchoolName,
    string SchoolAddress,
    string City,
    string District,
    string State,
    string Pincode,
    DateTime RegisteredAtUtc
);
