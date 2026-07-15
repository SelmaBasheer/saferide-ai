namespace SafeRide.Identity.Application.Auth.Register;

public sealed record RegisterSchoolAdminCommand(
    string FullName,
    string Email,
    string Phone,
    string Password,
    string SchoolName,
    string SchoolAddress,
    string City,
    string State,
    string Pincode
);
