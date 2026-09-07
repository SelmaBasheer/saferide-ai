using SafeRide.Identity.Domain.Enums;

namespace SafeRide.Identity.Domain.Entities;

public class OtpCode
{
    // A six-digit code has a million possibilities. Capping guesses per code is
    // the only limit an attacker cannot evade — rate limits partition by IP, and
    // IPs can be rotated.
    private const int MaxAttempts = 5;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!; // hashed, never the raw code
    public OtpPurpose Purpose { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public int Attempts { get; private set; }

    private OtpCode() { }

    public static OtpCode Issue(
        Guid userId,
        string codeHash,
        OtpPurpose purpose,
        int lifetimeMinutes = 5
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash,
            Purpose = purpose,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            CreatedAtUtc = DateTime.UtcNow,
        };

    public bool IsValid =>
        ConsumedAtUtc is null && Attempts < MaxAttempts && DateTime.UtcNow < ExpiresAtUtc;

    public bool AttemptsExhausted => Attempts >= MaxAttempts;

    public void RecordFailedAttempt() => Attempts++;

    public void Consume() => ConsumedAtUtc = DateTime.UtcNow;
}
