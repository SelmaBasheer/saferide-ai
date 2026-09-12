namespace SafeRide.Ai.Application.Anomalies.RecordDeviation;

public sealed record RecordDeviationCommand(
    Guid TripId,
    Guid SchoolId,
    Guid BusId,
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
