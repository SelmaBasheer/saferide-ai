using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Abstractions;

public sealed record AnomalyContext(
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
