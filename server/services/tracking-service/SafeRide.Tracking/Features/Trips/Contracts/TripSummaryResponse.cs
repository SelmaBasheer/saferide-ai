namespace SafeRide.Tracking.Features.Trips.Contracts;

public sealed record TripSummaryResponse(
    Guid Id,
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    string Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    string RouteCode,
    string RouteName,
    LastPositionResponse? LastPosition,
    int StudentCount,
    int UnmarkedCount
);
