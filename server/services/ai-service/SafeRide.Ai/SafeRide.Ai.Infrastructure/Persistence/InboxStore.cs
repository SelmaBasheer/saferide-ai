using Microsoft.EntityFrameworkCore;

namespace SafeRide.Ai.Infrastructure.Persistence;

public sealed class InboxStore(AiDbContext context)
{
    public Task<bool> HasProcessedAsync(Guid eventId, CancellationToken ct = default) =>
        context.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    /// Called only once the work is genuinely finished. Writing the receipt any
    /// earlier would mean a half-done message could never be retried, which is
    /// exactly the bug this whole exercise exists to fix.
    public async Task MarkProcessedAsync(
        Guid eventId,
        string eventType,
        CancellationToken ct = default
    )
    {
        context.ProcessedEvents.Add(
            new ProcessedEvent
            {
                EventId = eventId,
                EventType = eventType,
                ProcessedAtUtc = DateTime.UtcNow,
            }
        );

        await context.SaveChangesAsync(ct);
    }
}
