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
