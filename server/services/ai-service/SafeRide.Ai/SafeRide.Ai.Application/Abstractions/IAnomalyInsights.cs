namespace SafeRide.Ai.Application.Abstractions;

/// Counts and labels only. There is deliberately no way to ask this for a
/// person: the model cannot request a student's name because nothing here is
/// capable of returning one.
public sealed record AnomalyHistory(
    int Total,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByClassification
);

/// One line of the route report: how many anomalies of a kind, with a given
/// explanation, on a given route.
public sealed record RouteAnomalyReportLine(
    string RouteCode,
    string RouteName,
    string Type,
    string? Classification,
    int Total
);

/// Read-only history the classifier may consult before deciding. Every method
/// takes schoolId as its first argument because no lookup is allowed to cross
/// a school boundary, whatever the caller asks for.
public interface IAnomalyInsights
{
    Task<AnomalyHistory> ForRouteAsync(
        Guid schoolId,
        string routeCode,
        int days,
        CancellationToken ct = default
    );

    Task<AnomalyHistory> ForBusAsync(
        Guid schoolId,
        Guid busId,
        int days,
        CancellationToken ct = default
    );

    Task<AnomalyHistory> ForTripAsync(Guid schoolId, Guid tripId, CancellationToken ct = default);

    /// Backed by a stored procedure rather than LINQ — see AnomalyInsights for
    /// why this one query is different from its neighbours.
    Task<IReadOnlyList<RouteAnomalyReportLine>> ReportAsync(
        Guid schoolId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default
    );
}
