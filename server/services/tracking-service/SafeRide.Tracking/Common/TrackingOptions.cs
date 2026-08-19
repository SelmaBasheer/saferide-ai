namespace SafeRide.Tracking.Common;

public sealed class TrackingOptions
{
    public double GeofenceRadiusMetres { get; init; } = 100;
    public int ApproachStopsAhead { get; init; } = 2;
    public int StaleSignalSeconds { get; init; } = 60;
    public double DeviationThresholdMetres { get; init; } = 500;
    public int DeviationCooldownMinutes { get; init; } = 10;
}
