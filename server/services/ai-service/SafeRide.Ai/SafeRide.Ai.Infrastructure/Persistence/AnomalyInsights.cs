using Microsoft.EntityFrameworkCore;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Domain.Entities;

namespace SafeRide.Ai.Infrastructure.Persistence;

public sealed class AnomalyInsights(AiDbContext context) : IAnomalyInsights
{
    public Task<AnomalyHistory> ForRouteAsync(
        Guid schoolId,
        string routeCode,
        int days,
        CancellationToken ct = default
    )
    {
        var since = Since(days);

        return SummariseAsync(
            context.Anomalies.Where(a =>
                a.SchoolId == schoolId && a.RouteCode == routeCode && a.DetectedAtUtc >= since
            ),
            ct
        );
    }

    public Task<AnomalyHistory> ForBusAsync(
        Guid schoolId,
        Guid busId,
        int days,
        CancellationToken ct = default
    )
    {
        var since = Since(days);

        return SummariseAsync(
            context.Anomalies.Where(a =>
                a.SchoolId == schoolId && a.BusId == busId && a.DetectedAtUtc >= since
            ),
            ct
        );
    }

    public Task<AnomalyHistory> ForTripAsync(
        Guid schoolId,
        Guid tripId,
        CancellationToken ct = default
    ) =>
        SummariseAsync(
            context.Anomalies.Where(a => a.SchoolId == schoolId && a.TripId == tripId),
            ct
        );

    /// Clamped so a model asking for a year of history cannot turn a tool call
    /// into a table scan.
    private static DateTime Since(int days) => DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 90));

    private static async Task<AnomalyHistory> SummariseAsync(
        IQueryable<Anomaly> query,
        CancellationToken ct
    )
    {
        // Grouped in SQL, not in memory: the answer is a handful of counts, so
        // there is no reason to bring the rows back to count them here.
        var rows = await query
            .GroupBy(a => new { a.Type, a.Classification })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.Classification,
                Count = g.Count(),
            })
            .ToListAsync(ct);

        var byType = rows.GroupBy(r => r.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Count));

        var byClassification = rows.Where(r => !string.IsNullOrWhiteSpace(r.Classification))
            .GroupBy(r => r.Classification!)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Count));

        return new AnomalyHistory(rows.Sum(r => r.Count), byType, byClassification);
    }
}
