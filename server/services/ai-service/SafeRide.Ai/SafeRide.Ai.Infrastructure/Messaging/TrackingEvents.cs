namespace SafeRide.Ai.Infrastructure.Messaging;

public static class TrackingRoutingKeys
{
    public const string RouteDeviationDetected = "route-deviation-detected";
    public const string StopsSkipped = "stops-skipped";
}

public sealed record RouteDeviationDetectedEvent(
    Guid EventId,
    Guid TripId,
    Guid SchoolId,
    Guid BusId,
    Guid DriverId,
    string RouteCode,
    string RouteName,
    double Latitude,
    double Longitude,
    double MetresOffRoute,
    double? SpeedKmh,
    int StopsTotal,
    int StopsReached,
    DateTime TripStartedAt,
    DateTime OccurredAtUtc
);

public sealed record SkippedStopInfo(int Sequence, string Name, string PickupTime);

public sealed record StopsSkippedEvent(
    Guid EventId,
    Guid TripId,
    Guid SchoolId,
    Guid BusId,
    Guid DriverId,
    string RouteCode,
    string RouteName,
    int StopsTotal,
    int StopsReached,
    IReadOnlyList<SkippedStopInfo> SkippedStops,
    DateTime TripStartedAt,
    DateTime TripEndedAt,
    DateTime OccurredAtUtc
);
