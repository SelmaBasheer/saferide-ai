using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Enums;

namespace SafeRide.Identity.Domain.Repositories;

public interface IOtpCodeRepository
{
    Task AddAsync(OtpCode otp, CancellationToken ct = default);

    // most recent OTP for this user + purpose (for cooldown + verification)
    Task<OtpCode?> GetLatestAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default);

    // A newly issued code makes older ones for the same purpose useless. Removing them keeps only one code valid at a time, which halves an attacker's surface.
    Task<int> DeleteForUserAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default);

    // Consumed or expired codes older than the retention window.
    Task<int> DeleteStaleAsync(DateTime cutoffUtc, CancellationToken ct = default);
}
