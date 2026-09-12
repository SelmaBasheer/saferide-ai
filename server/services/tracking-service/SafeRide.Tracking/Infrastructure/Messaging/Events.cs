namespace SafeRide.Tracking.Infrastructure.Messaging;

public sealed record TripStartedEvent(
    Guid TripId,
    Guid SchoolId,
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    string RouteCode,
    int StudentCount,
    DateTime OccurredAtUtc
);

public sealed record TripEndedEvent(
    Guid TripId,
    Guid SchoolId,
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    int BoardedCount,
    int AbsentCount,
    int UnmarkedCount,
    DateTime OccurredAtUtc
);

public sealed record StudentBoardedEvent(
    Guid TripId,
    Guid SchoolId,
    Guid StudentId,
    Guid StopId,
    string StopName,
    string Status,
    DateTime OccurredAtUtc
);

public sealed record RouteDeviationDetected(
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
