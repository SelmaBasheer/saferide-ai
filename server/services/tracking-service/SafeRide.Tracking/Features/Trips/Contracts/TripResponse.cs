namespace SafeRide.Tracking.Features.Trips.Contracts;

public sealed record GeoPointResponse(double Latitude, double Longitude);

public sealed record TripStopResponse(
    Guid StopId,
    int Sequence,
    string Name,
    double Latitude,
    double Longitude,
    string PickupTime,
    DateTime? ReachedAt
);

public sealed record TripRosterResponse(
    Guid StudentId,
    string Name,
    Guid PickupStopId,
    string BoardingStatus,
    DateTime? MarkedAt
);

public sealed record LastPositionResponse(
    double Latitude,
    double Longitude,
    DateTime RecordedAt,
    double? SpeedKmh,
    string Source
);

public sealed record TripResponse(
    Guid Id,
    Guid SchoolId,
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    string Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    string RouteCode,
    string RouteName,
    List<TripStopResponse> Stops,
    List<GeoPointResponse> Path,
    List<TripRosterResponse> Roster,
    LastPositionResponse? LastPosition,
    int UnmarkedCount
);
