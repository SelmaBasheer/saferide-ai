using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Abstractions;

public interface ISchoolReportRepository
{
    Task<SchoolReport> BuildAsync(
        Guid schoolId,
        ReportRange range,
        SchoolReportSection section,
        CancellationToken ct
    );
}
