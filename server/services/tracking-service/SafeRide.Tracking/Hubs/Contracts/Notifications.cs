namespace SafeRide.Tracking.Hubs.Contracts;

public sealed record PositionUpdate(
    Guid TripId,
    Guid BusId,
    string RouteCode,
    double Latitude,
    double Longitude,
    double? SpeedKmh,
    DateTime RecordedAt,
    string Source
);

public sealed record TripLifecycleNotification(
    Guid TripId,
    Guid BusId,
    string RouteCode,
    string RouteName,
    DateTime At
);

public sealed record StopReachedNotification(
    Guid TripId,
    Guid StopId,
    string StopName,
    int Sequence,
    DateTime At
);

public sealed record ApproachingStopNotification(
    Guid TripId,
    string RouteCode,
    Guid StopId,
    string StopName,
    int Sequence,
    int StopsAway
);

public sealed record StudentBoardedNotification(
    Guid TripId,
    Guid StudentId,
    string StudentName,
    string StopName,
    string Status,
    DateTime At
);

public sealed record RouteDeviationNotification(
    Guid TripId,
    Guid BusId,
    string RouteCode,
    double Latitude,
    double Longitude,
    double MetresOffRoute,
    DateTime At
);
