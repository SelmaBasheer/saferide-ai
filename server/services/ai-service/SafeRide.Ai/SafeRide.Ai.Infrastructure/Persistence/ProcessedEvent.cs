namespace SafeRide.Ai.Infrastructure.Persistence;

/// The receipt book: one row per message this service has *completely* finished.
/// It lives in Infrastructure rather than Domain on purpose — "which messages
/// have I already seen" is a fact about our plumbing, not about school buses.
public sealed class ProcessedEvent
{
    public Guid EventId { get; init; }

    public string EventType { get; init; } = string.Empty;

    public DateTime ProcessedAtUtc { get; init; }
}
