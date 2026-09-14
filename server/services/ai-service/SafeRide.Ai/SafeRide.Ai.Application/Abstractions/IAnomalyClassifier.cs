using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Abstractions;

/// SchoolId, TripId and BusId are carried here so the classifier can look up
/// history. Note that they come from the anomaly being classified — the model
/// never supplies them, so it cannot ask about another school's buses even if
/// it tried.
public sealed record AnomalyContext(
    Guid SchoolId,
    Guid TripId,
    Guid BusId,
    AnomalyType Type,
    string RouteCode,
    string RouteName,
    string ContextJson
);

public sealed record Classification(
    string Label,
    string Reasoning,
    string DraftMessage,
    double? Confidence,
    bool ByModel
);

public interface IAnomalyClassifier
{
    Task<Classification> ClassifyAsync(AnomalyContext context, CancellationToken ct = default);
}
