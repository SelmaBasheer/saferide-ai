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

/// <summary>
/// One student's outcome on one trip, snapshotted as the trip ended. Name and
/// stop are copied rather than referenced: a child who changes route in October
/// must not appear on the new route in September's report.
/// </summary>
public sealed record TripRosterEntry(
    Guid StudentId,
    string Name,
    Guid? PickupStopId,
    string? StopName,
    string BoardingStatus,
    DateTime? MarkedAt
);

/// <summary>
/// A completed trip and everyone who was on it.
///
/// Deliberately fat. A consumer building a report must never have to call back
/// for the route's name or the roster — that would reintroduce the coupling the
/// event exists to avoid, and it would give the wrong answer once a route is
/// renamed.
///
/// Carries the whole roster rather than relying on StudentBoardedEvent, because
/// a student nobody marked produces no boarding event at all — and an unmarked
/// child is exactly what an attendance report needs to show.
/// </summary>
public sealed record TripEndedEvent(
    Guid TripId,
    Guid SchoolId,
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    string RouteCode,
    string RouteName,
    DateTime StartedAt,
    DateTime EndedAt,
    int StudentCount,
    int BoardedCount,
    int AbsentCount,
    int UnmarkedCount,
    IReadOnlyList<TripRosterEntry> Roster,
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
