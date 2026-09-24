using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafeRide.Schools.Application.Subscriptions.Command;

namespace SafeRide.Schools.Infrastructure.Jobs;

/// <summary>
/// Runs the warning-and-expiry check hourly.
///
/// Hourly rather than daily-at-a-fixed-time because the work is idempotent, and
/// that removes every question about what happens if the process restarts at
/// 3am, or which timezone "daily" means. A warning is sent at most once per day
/// because the subscription records the date it last went out.
///
/// With more than one instance of this service, every instance runs it. Harmless
/// here — an already-sent warning and an already-suspended school are both
/// skipped — but a job whose side effects were not idempotent would need a
/// distributed lock, the way Tracking uses Hangfire for its deviation check.
/// </summary>
public sealed class SubscriptionExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionExpiryWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Once on startup, so a service that has been down over a weekend
        // catches up immediately rather than an hour later.
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            // A fresh scope each run: the handler is scoped, and a DbContext
            // held for the lifetime of the process would accumulate every
            // entity it ever loaded.
            using var scope = scopeFactory.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService<ExpireSubscriptionsHandler>();

            var outcome = await handler.RunAsync(ct);

            if (outcome.Warned > 0)
                logger.LogInformation(
                    "Sent {Count} subscription expiry warning(s)",
                    outcome.Warned
                );

            if (outcome.Suspended.Count > 0)
                logger.LogWarning(
                    "Suspended {Count} school(s) for expired subscriptions: {SchoolIds}",
                    outcome.Suspended.Count,
                    string.Join(", ", outcome.Suspended)
                );
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // Swallowed on purpose. An unhandled exception here would take the
            // whole service down, and a database blip should not stop schools
            // logging in.
            logger.LogError(ex, "Subscription expiry run failed, will retry next hour");
        }
    }
}
