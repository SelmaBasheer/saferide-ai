using SafeRide.Identity.Domain.Enums;

namespace SafeRide.Identity.Application.Auth.Login;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    Guid UserId,
    string FirstName,
    string LastName,
    UserRole Role,
    bool MustChangePassword
);
