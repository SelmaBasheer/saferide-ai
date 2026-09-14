namespace SafeRide.Ai.Infrastructure.Persistence;

/// The shape the stored procedure returns. Keyless: EF maps the columns but
/// knows there is no table behind it and nothing to track or update.
public sealed class RouteAnomalyRow
{
    public string RouteCode { get; init; } = string.Empty;

    public string RouteName { get; init; } = string.Empty;

    /// The raw enum value, because SQL stores enums as numbers. Translated to a
    /// name before it leaves Infrastructure.
    public int Type { get; init; }

    public string? Classification { get; init; }

    public int Total { get; init; }
}
