using Microsoft.EntityFrameworkCore;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(IdentityDbContext dbContext) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await dbContext.RefreshTokens.AddAsync(refreshToken, ct);

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    ) => dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
}
