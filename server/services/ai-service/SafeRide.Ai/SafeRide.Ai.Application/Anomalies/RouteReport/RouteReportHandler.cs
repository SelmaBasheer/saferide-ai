using SafeRide.Ai.Application.Abstractions;

namespace SafeRide.Ai.Application.Anomalies.RouteReport;

public sealed class RouteReportHandler(IAnomalyInsights insights)
{
    private const int DefaultDays = 7;

    /// A quarter is the widest window worth allowing. Clamped rather than
    /// rejected, for the same reason page numbers are: an out-of-range window is
    /// a careless request, not a malicious one.
    private const int MaxDays = 92;

    public Task<IReadOnlyList<RouteAnomalyReportLine>> HandleAsync(
        Guid schoolId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct
    )
    {
        var to = toUtc ?? DateTime.UtcNow;
        var from = fromUtc ?? to.AddDays(-DefaultDays);

        if (from >= to)
        {
            from = to.AddDays(-DefaultDays);
        }

        if ((to - from).TotalDays > MaxDays)
        {
            from = to.AddDays(-MaxDays);
        }

        return insights.ReportAsync(schoolId, from, to, ct);
    }
}
