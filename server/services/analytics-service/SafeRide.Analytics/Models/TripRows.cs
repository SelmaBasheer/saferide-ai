namespace SafeRide.Analytics.Models;

public sealed record TripFactRow(
    Guid TripId,
    Guid SchoolId,
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    string RouteCode,
    string RouteName,
    string? DriverName,
    DateOnly TripDate,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int StudentCount,
    int BoardedCount,
    int AbsentCount,
    int UnmarkedCount
);

public sealed record AttendanceFactRow(
    Guid TripId,
    Guid StudentId,
    Guid SchoolId,
    DateOnly TripDate,
    string StudentName,
    Guid? StopId,
    string? StopName,
    string? RouteCode,
    string? RouteName,
    Guid BusId,
    string Status,
    DateTime? MarkedAtUtc
);
