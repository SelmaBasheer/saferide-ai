using SafeRide.Identity.Domain.Entities;

namespace SafeRide.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct);
}
