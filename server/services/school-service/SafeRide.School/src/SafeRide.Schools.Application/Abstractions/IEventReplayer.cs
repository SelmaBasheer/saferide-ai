namespace SafeRide.Schools.Application.Abstractions;

public sealed record ReplaySummary(
    int SchoolsApproved,
    int SchoolsSuspended,
    int Subscriptions,
    int Payments
);

/// <summary>
/// Re-publishes what already happened, so a projection that came online late
/// can catch up. Not an import: the same events travel the same path as the
/// live ones, so there is no second code path to drift out of step.
/// </summary>
public interface IEventReplayer
{
    Task<ReplaySummary> ReplayAsync(CancellationToken ct);
}
