using Microsoft.EntityFrameworkCore;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Infrastructure.Persistence.Repositories;

public sealed class OtpCodeRepository(IdentityDbContext context) : IOtpCodeRepository
{
    public async Task AddAsync(OtpCode otp, CancellationToken ct = default) =>
        await context.Set<OtpCode>().AddAsync(otp, ct);

    public Task<OtpCode?> GetLatestAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken ct = default
    ) =>
        context
            .Set<OtpCode>()
            .Where(o => o.UserId == userId && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
}
