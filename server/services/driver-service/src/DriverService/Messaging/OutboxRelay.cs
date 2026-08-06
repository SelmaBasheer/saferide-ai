using DriverService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Messaging;

public class OutboxRelay(
    IServiceScopeFactory scopeFactory,
    IEventPublisher publisher,
    ILogger<OutboxRelay> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DriverDbContext>();

                var pending = await db
                    .OutboxMessages.Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.OccurredAt) // preserve event order
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pending)
                {
                    await publisher.PublishAsync(
                        "driver.events",
                        msg.Type,
                        msg.Payload,
                        stoppingToken
                    );
                    msg.ProcessedAt = DateTime.UtcNow;
                    logger.LogInformation("Published outbox message {Type} {Id}", msg.Type, msg.Id);
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox relay cycle failed; will retry");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
