using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Enums;

namespace SafeRide.Identity.Domain.Repositories;

public interface IOtpCodeRepository
{
    Task AddAsync(OtpCode otp, CancellationToken ct = default);

    // most recent OTP for this user + purpose (for cooldown + verification)
    Task<OtpCode?> GetLatestAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default);
}
