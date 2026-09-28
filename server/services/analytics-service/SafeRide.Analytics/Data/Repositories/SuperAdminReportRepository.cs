using Dapper;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Data.Repositories;

public sealed class SuperAdminReportRepository(IDbConnectionFactory factory)
    : ISuperAdminReportRepository
{
    public async Task<SuperAdminReport> BuildAsync(ReportRange range, CancellationToken ct)
    {
        using var conn = factory.Create();

        // Four round trips rather than one multi-result query. Dapper's
        // QueryMultiple would save three, but the four scripts stay separately
        // readable and runnable in SSMS — worth more here than the milliseconds.
        var parameters = new
        {
            range.From,
            range.To,
            Today = DateOnly.FromDateTime(DateTime.UtcNow),
        };

        var summary = await conn.QuerySingleAsync<SuperAdminSummary>(
            new CommandDefinition(
                SqlScripts.Get("ReportSuperAdminSummary"),
                parameters,
                cancellationToken: ct
            )
        );

        var schools = await conn.QueryAsync<SchoolReportRow>(
            new CommandDefinition(
                SqlScripts.Get("ReportSuperAdminSchools"),
                parameters,
                cancellationToken: ct
            )
        );

        var revenue = await conn.QueryAsync<RevenueReportRow>(
            new CommandDefinition(
                SqlScripts.Get("ReportSuperAdminRevenue"),
                parameters,
                cancellationToken: ct
            )
        );

        var plans = await conn.QueryAsync<PlanReportRow>(
            new CommandDefinition(
                SqlScripts.Get("ReportSuperAdminPlans"),
                parameters,
                cancellationToken: ct
            )
        );

        return new SuperAdminReport(
            range,
            summary,
            schools.ToList(),
            revenue.ToList(),
            plans.ToList()
        );
    }
}
