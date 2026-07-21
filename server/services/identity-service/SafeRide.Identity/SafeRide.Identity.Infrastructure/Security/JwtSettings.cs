namespace SafeRide.Identity.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; init; } = default!;
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public int AccessTokenMinutes { get; init; } = default!;
    public int RefreshTokenDays { get; init; } = default!;
}
