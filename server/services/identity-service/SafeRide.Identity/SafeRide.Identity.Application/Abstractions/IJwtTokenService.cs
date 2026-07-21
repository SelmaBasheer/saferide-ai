using SafeRide.Identity.Domain.Entities;

namespace SafeRide.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    TimeSpan AccessTokenLifetime { get; }
    TimeSpan RefreshTokenLifetime { get; }
}
