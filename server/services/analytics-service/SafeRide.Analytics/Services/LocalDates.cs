namespace SafeRide.Analytics.Services;

/// Report ranges are asked in the school's own calendar, not in UTC. Converting
/// in one place means a trip and a payment on the same morning land on the same
/// date, which they would not if each consumer did its own arithmetic.
public sealed class LocalDates
{
    private readonly TimeZoneInfo _zone;

    public LocalDates(IConfiguration config) =>
        _zone = TimeZoneInfo.FindSystemTimeZoneById(config["Saferide:Timezone"] ?? "Asia/Kolkata");

    public DateOnly ToLocalDate(DateTime utc) =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zone)
        );
}
