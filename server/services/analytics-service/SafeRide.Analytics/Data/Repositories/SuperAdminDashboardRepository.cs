using Dapper;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Models;
using SafeRide.Analytics.Services;

namespace SafeRide.Analytics.Data.Repositories;

public sealed class SuperAdminDashboardRepository(
    IDbConnectionFactory factory,
    LocalDates localDates
) : ISuperAdminDashboardRepository
{
    /// Matches Subscription.GraceDays in the School service.
    private const int GraceDays = 7;

    /// A month's warning is long enough for a school to act on a renewal.
    private const int ExpiringSoonDays = 30;

    /// Panel lists, not pages. "Show me more" is a link, not a longer list.
    private const int RecentTake = 8;

    public async Task<SuperAdminDashboard> BuildAsync(ReportRange range, CancellationToken ct)
    {
        // Every boundary is the school's local date, not UTC. A payment at 2am
        // in Kolkata belongs to that day, and "this month" has to agree with
        // the calendar on the admin's wall.
        var today = localDates.ToLocalDate(DateTime.UtcNow);

        // One parameter set for every query. Each script takes the handful it
        // needs and ignores the rest, which keeps the range and the "today"
        // boundaries defined in exactly one place.
        var parameters = new
        {
            Today = today,
            SoonCutoff = today.AddDays(ExpiringSoonDays),
            GraceCutoff = today.AddDays(-GraceDays),
            MonthStart = new DateOnly(today.Year, today.Month, 1),
            YearStart = new DateOnly(today.Year, 1, 1),
            range.From,
            range.To,
            Take = RecentTake,
        };

        using var conn = factory.Create();

        // Subscription lifecycle is current state and cannot be ranged: it is
        // derived from subscription_ends_on against today, and dim_school keeps
        // no history, so "active in March" would need a nightly snapshot table.
        var subscriptions = await conn.QuerySingleAsync<SubscriptionCounts>(
            Command("DashboardSubscriptions", parameters, ct)
        );

        // Returns both — platform totals as they stand today, and schools that
        // came on board inside the range counted by their status now.
        var schools = await conn.QuerySingleAsync<SchoolCounts>(
            Command("DashboardSchools", parameters, ct)
        );

        var revenue = await conn.QuerySingleAsync<RevenueTotals>(
            Command("DashboardRevenue", parameters, ct)
        );

        // Everything below describes things that happened, so all of it honours
        // the range.
        var byMonth = await conn.QueryAsync<MonthlyRevenue>(
            Command("DashboardRevenueByMonth", parameters, ct)
        );

        var plans = await conn.QueryAsync<PlanReportRow>(
            Command("DashboardRevenueByPlan", parameters, ct)
        );

        var recentSchools = await conn.QueryAsync<RecentSchool>(
            Command("DashboardRecentSchools", parameters, ct)
        );

        var recentPurchases = await conn.QueryAsync<RecentPurchase>(
            Command("DashboardRecentPurchases", parameters, ct)
        );

        return new SuperAdminDashboard(
            range,
            subscriptions,
            schools,
            revenue,
            byMonth.ToList(),
            plans.ToList(),
            recentSchools.ToList(),
            recentPurchases.ToList()
        );
    }

    private static CommandDefinition Command(
        string script,
        object parameters,
        CancellationToken ct
    ) => new(SqlScripts.Get(script), parameters, cancellationToken: ct);
}
