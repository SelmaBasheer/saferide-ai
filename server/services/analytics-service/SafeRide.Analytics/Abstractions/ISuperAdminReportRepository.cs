using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Abstractions;

public interface ISuperAdminReportRepository
{
    Task<SuperAdminReport> BuildAsync(ReportRange range, CancellationToken ct);
}
