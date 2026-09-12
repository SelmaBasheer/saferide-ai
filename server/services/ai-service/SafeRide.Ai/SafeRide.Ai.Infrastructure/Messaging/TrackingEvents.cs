namespace SafeRide.Ai.Infrastructure.Messaging;

public static class TrackingRoutingKeys
{
    public const string RouteDeviationDetected = "route-deviation-detected";
}

public sealed record RouteDeviationDetectedEvent(
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
