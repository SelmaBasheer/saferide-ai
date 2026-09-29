namespace SafeRide.Analytics.Messaging.Events;

/// Mirrors the Tracking service's contract. Name and stop arrive already
/// snapshotted, so nothing here has to look anything up.
public sealed record TripRosterEntry(
    Guid StudentId,
    string Name,
    Guid? PickupStopId,
    string? StopName,
    string BoardingStatus,
    DateTime? MarkedAt
);

public sealed record TripEnded(
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
