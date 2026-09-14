namespace SafeRide.Ai.Infrastructure.Persistence;

/// One row per status change, written by a database trigger rather than by the
/// application. Infrastructure rather than Domain: "who changed what, when" is a
/// fact about our record-keeping, not about school buses.
public sealed class AnomalyAudit
{
    public long Id { get; init; }
    public Guid AnomalyId { get; init; }
    public Guid SchoolId { get; init; }
    public int OldStatus { get; init; }
    public int NewStatus { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public DateTime ChangedAtUtc { get; init; }
}
