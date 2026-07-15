namespace SafeRide.Identity.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreationAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(Guid userId, string tokenHash, int lifetimeDays = 7) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(lifetimeDays),
            CreationAtUtc = DateTime.UtcNow,
        };

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;

    public void Revoke() => RevokedAtUtc = DateTime.UtcNow;
}
