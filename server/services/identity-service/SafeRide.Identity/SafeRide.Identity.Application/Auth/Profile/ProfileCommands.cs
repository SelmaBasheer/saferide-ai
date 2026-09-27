namespace SafeRide.Identity.Application.Auth.Profile;

public sealed record UpdateProfileCommand(string FirstName, string LastName, string Phone);

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);

/// What the API returns for "who am I". Built in the application layer rather
/// than the controller, because the photo URL needs blob storage and a
/// controller has no business knowing about it.
public sealed record ProfileView(
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
