namespace SafeRide.Ai.Application.Abstractions;

/// How many messages are parked, and where.
public sealed record DeadLetterStatus(string Queue, uint MessageCount);

/// One dead-lettered message, described without consuming it. Reason and
/// DeathCount come from RabbitMQ's own x-death header, not from a guess.
public sealed record DeadLetterMessage(
    string OriginalRoutingKey,
    string Reason,
    long DeathCount,
    string Body
);

public interface IDeadLetterQueue
{
    Task<DeadLetterStatus> GetStatusAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DeadLetterMessage>> PeekAsync(int max, CancellationToken ct = default);

    /// Republishes messages to the main exchange. Deliberately manual: a message
    /// only reaches the DLQ after already failing three times, so replaying on a
    /// timer would just be a slower infinite loop.
    Task<int> ReplayAsync(int max, CancellationToken ct = default);
}
