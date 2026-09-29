using Dapper;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Data.Repositories;

public sealed class SchoolReportRepository(IDbConnectionFactory factory) : ISchoolReportRepository
{
    public async Task<SchoolReport> BuildAsync(
        Guid schoolId,
        ReportRange range,
        SchoolReportSection section,
        CancellationToken ct
    )
    {
        using var conn = factory.Create();

        var parameters = new
        {
            SchoolId = schoolId,
            range.From,
            range.To,
        };

        // The summary is always returned — it drives the cards and the chart,
        // and it costs one aggregate regardless of which section is on screen.
        var summary = await conn.QuerySingleAsync<SchoolReportSummary>(
            new CommandDefinition(
                SqlScripts.Get("ReportSchoolSummary"),
                parameters,
                cancellationToken: ct
            )
        );

        // Only the chosen section's rows are fetched. Attendance over a year is
        // thousands of rows, and loading them to render a trip table would be
        // work nobody asked for.
        IReadOnlyList<AttendanceReportRow> attendance = [];
        IReadOnlyList<TripReportRow> trips = [];

        if (section == SchoolReportSection.Attendance)
        {
            attendance =
            [
                .. await conn.QueryAsync<AttendanceReportRow>(
                    new CommandDefinition(
                        SqlScripts.Get("ReportSchoolAttendance"),
                        parameters,
                        cancellationToken: ct
                    )
                ),
            ];
        }
        else
        {
            trips =
            [
                .. await conn.QueryAsync<TripReportRow>(
                    new CommandDefinition(
                        SqlScripts.Get("ReportSchoolTrips"),
                        parameters,
                        cancellationToken: ct
                    )
                ),
            ];
        }

        return new SchoolReport(range, section, summary, attendance, trips);
    }
}
