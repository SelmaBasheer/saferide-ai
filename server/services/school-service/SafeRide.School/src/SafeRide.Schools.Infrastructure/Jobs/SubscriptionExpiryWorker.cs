using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafeRide.Schools.Application.Subscriptions.Command;

namespace SafeRide.Schools.Infrastructure.Jobs;

/// <summary>
/// Runs the warning-and-expiry check once a day.
///
/// Daily rather than hourly because the work is idempotent, so twelve extra
/// runs a day would only find nothing. A warning is sent at most once per day
/// regardless, because the subscription records the date it last went out.
///
/// The timer counts 24 hours from process start, so a redeploy moves the time
/// of day it runs. That does not matter: a school suspended a few hours later
/// than another has already been a week overdue.
///
/// With more than one instance, every instance runs it. Harmless here — an
/// already-sent warning and an already-suspended school are both skipped — but
/// a job whose side effects were not idempotent would need a distributed lock,
/// the way Tracking uses Hangfire for its deviation check.
/// /// </summary>
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
            using var scope = scopeFactory.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService<ExpireSubscriptionsHandler>();

            var outcome = await handler.RunAsync(ct);

            if (outcome.Warned > 0)
                logger.LogInformation(
                    "Sent {Count} subscription expiry warning(s)",
                    outcome.Warned
                );

            if (outcome.WarningsFailed)
                logger.LogError(
                    "The expiry warning pass failed. Suspensions still ran; warnings will retry tomorrow."
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
            logger.LogError(ex, "Subscription expiry run failed, will retry tomorrow");
        }
    }
}
