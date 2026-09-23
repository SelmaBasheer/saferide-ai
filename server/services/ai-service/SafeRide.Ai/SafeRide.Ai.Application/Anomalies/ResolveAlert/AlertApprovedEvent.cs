namespace SafeRide.Ai.Application.Anomalies.ResolveAlert;

/// <summary>
/// Announced when a human approves an alert for sending.
///
/// It carries the recipient's address rather than only a school id. The
/// Notification service holds no database and cannot look anyone up, so
/// whoever publishes an event is responsible for saying who it is for.
/// </summary>
public sealed record AlertApprovedEvent(
    Guid EventId,
    Guid SchoolId,
    Guid AlertId,
    string RouteCode,
    string RouteName,
    string AnomalyType,
    string Message,
    string RecipientEmail,
    DateTime OccurredAtUtc
)
{
    public const string RoutingKey = "alert-approved";
}
