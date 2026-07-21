using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Application.Auth.Register;

public sealed record RegisterSchoolAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password,
    string SchoolName,
    string SchoolAddress,
    string City,
    string District,
    string State,
    string Pincode
);
