using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Abstractions;

public interface ISuperAdminDashboardRepository
{
    Task<SuperAdminDashboard> BuildAsync(ReportRange range, CancellationToken ct);
}
