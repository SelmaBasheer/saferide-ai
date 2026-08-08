namespace DriverService.Persistence;

// Read model of School service's approval state, maintained by events.
// Deliberately a plain mutable row, not a domain entity — it has no behavior, only truth-tracking.
public class SchoolStatusProjection
{
    public Guid SchoolId { get; set; }
    public string Status { get; set; } = null!; // "Approved" | "Suspended"
    public DateTime UpdatedAt { get; set; }

    public DateTime EventAtUtc { get; set; } // source event time — guards against stale replays
}
