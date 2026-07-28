namespace SafeRide.Schools.Api.Contracts;

public sealed record SchoolDto(
    Guid Id,
    string Name,
    string City,
    string District,
    string State,
    string Pincode,
    string AdminName,
    string AdminEmail,
    string Status,
    DateTime CreatedAtUtc
);
