namespace SafeRide.Analytics.Models;

public enum SchoolReportSection
{
    Attendance,
    Trips,
}

public sealed record SchoolReportSummary(int Trips, int Boarded, int Absent, int Unmarked)
{
    /// Unmarked students are excluded from the denominator, not counted as
    /// present. Nobody knows whether they boarded, and guessing would make the
    /// figure look better than the truth.
    public decimal AttendanceRate =>
        Boarded + Absent == 0 ? 0 : Math.Round(100m * Boarded / (Boarded + Absent), 1);
}

public sealed record AttendanceReportRow(
    DateOnly TripDate,
    string? RouteCode,
    string? RouteName,
    string StudentName,
    string? StopName,
    string Status,
    DateTime? MarkedAtUtc
);

public sealed record TripReportRow(
    DateOnly TripDate,
    string? RouteCode,
    string? RouteName,
    string? BusRegistration,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int StudentCount,
    int BoardedCount,
    int AbsentCount,
    int UnmarkedCount
);

public sealed record SchoolReport(
    ReportRange Range,
    SchoolReportSection Section,
    SchoolReportSummary Summary,
    IReadOnlyList<AttendanceReportRow> Attendance,
    IReadOnlyList<TripReportRow> Trips
);
