namespace SafeRide.Identity.Api.Contracts;

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Phone,
    string Role,
    string Status,
    Guid? SchoolId,
    bool MustChangePassword,
    string? PhotoUrl
);

public sealed record UpdateProfileRequest(string FirstName, string LastName, string Phone);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
