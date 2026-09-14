using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Anomalies.RecordAnomaly;

/// The shape this service works in, deliberately not Tracking's event type.
/// Every kind of anomaly arrives the same way: where it happened, what kind it
/// was, and the raw context the model will read. Adding a sixth detector means
/// a new AnomalyType and nothing else here.
public sealed record RecordAnomalyCommand(
    Guid SchoolId,
    Guid TripId,
    Guid BusId,
    string RouteCode,
    string RouteName,
    AnomalyType Type,
    string ContextJson,
    DateTime OccurredAtUtc
);
