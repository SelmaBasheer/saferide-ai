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

/// EventId identifies this message, not this incident. A consumer uses it to
/// answer one question: "have I already finished this exact message?"
public sealed record RouteDeviationDetected(
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

/// A stop the bus never reached. Places, not people — no child is named here,
/// and nothing downstream can infer one from it.
public sealed record SkippedStopInfo(int Sequence, string Name, string PickupTime);

/// One event per trip rather than one per stop: three missed stops on a single
/// run is one story for the school office, not three separate alarms.
public sealed record StopsSkippedDetected(
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
