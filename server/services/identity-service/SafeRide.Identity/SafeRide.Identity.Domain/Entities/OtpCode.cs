using SafeRide.Identity.Domain.Enums;

namespace SafeRide.Identity.Domain.Entities;

public class OtpCode
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!; // hashed, never the raw code
    public OtpPurpose Purpose { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }

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

    public bool IsValid => ConsumedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;

    public void Consume() => ConsumedAtUtc = DateTime.UtcNow;
}
