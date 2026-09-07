using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Infrastructure.BackgroundServices;

/// Removes OTP codes that are consumed or long expired. Deliberately a plain
/// hosted service rather than Hangfire: this job needs no dashboard, no retry,
/// no manual trigger and no persistence — if a run is missed, the next one
/// catches everything. Hangfire earns its place in Tracking because the
/// deviation check needs all four.
public sealed class OtpCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OtpCleanupService> logger
) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    // Keep expired codes briefly so "I never got a code" can still be diagnosed.
    // Only the hash is ever stored, never the code itself.
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var otps = scope.ServiceProvider.GetRequiredService<IOtpCodeRepository>();

                var cutoff = DateTime.UtcNow - Retention;
                var removed = await otps.DeleteStaleAsync(cutoff, stoppingToken);

                if (removed > 0)
                {
                    logger.LogInformation(
                        "Removed {Count} OTP codes older than {Cutoff}",
                        removed,
                        cutoff
                    );
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OTP cleanup run failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
