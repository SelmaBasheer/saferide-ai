using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Common;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Services;

public sealed class SchoolReportService(
    ISchoolReportRepository repository,
    ITenantProvider tenant,
    LocalDates localDates
)
{
    public ReportRange ResolveRange(DateOnly? from, DateOnly? to) =>
        ReportRanges.Resolve(from, to, localDates.ToLocalDate(DateTime.UtcNow));

    public Task<SchoolReport> BuildAsync(
        ReportRange range,
        SchoolReportSection section,
        CancellationToken ct
    )
    {
        // Never from the request. A school admin whose token carries no schoolId
        // has nothing to report on, and accepting one from the caller would let
        // any school read any other's attendance.
        var schoolId =
            tenant.SchoolId
            ?? throw AppException.BadRequest(
                "Report.NoSchool",
                "This account is not linked to a school."
            );

        return repository.BuildAsync(schoolId, range, section, ct);
    }

    public static byte[] ToCsv(SchoolReport report)
    {
        var csv = new CsvBuilder();

        csv.Section($"SafeRide AI — {report.Section} report")
            .Row("Range", $"{report.Range.From:yyyy-MM-dd} to {report.Range.To:yyyy-MM-dd}")
            .Row("Generated", DateTime.UtcNow);

        csv.Section("Summary")
            .Row("Trips", report.Summary.Trips)
            .Row("Boarded", report.Summary.Boarded)
            .Row("Absent", report.Summary.Absent)
            .Row("Unmarked", report.Summary.Unmarked)
            .Row("Attendance rate (%)", report.Summary.AttendanceRate);

        if (report.Section == SchoolReportSection.Attendance)
        {
            csv.Section("Attendance")
                .Row("Date", "Route", "Student", "Stop", "Status", "Marked at");

            foreach (var r in report.Attendance)
            {
                csv.Row(
                    r.TripDate,
                    r.RouteCode,
                    r.StudentName,
                    r.StopName,
                    r.Status,
                    r.MarkedAtUtc
                );
            }
        }
        else
        {
            csv.Section("Trips")
                .Row(
                    "Date",
                    "Route",
                    "Bus",
                    "Started",
                    "Ended",
                    "Students",
                    "Boarded",
                    "Absent",
                    "Unmarked"
                );

            foreach (var t in report.Trips)
            {
                csv.Row(
                    t.TripDate,
                    t.RouteCode,
                    t.BusRegistration,
                    t.StartedAtUtc,
                    t.EndedAtUtc,
                    t.StudentCount,
                    t.BoardedCount,
                    t.AbsentCount,
                    t.UnmarkedCount
                );
            }
        }

        return csv.ToBytes();
    }
}
