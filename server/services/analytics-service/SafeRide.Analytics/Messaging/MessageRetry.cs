using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SafeRide.Analytics.Messaging;

/// <summary>
/// One retry policy for every consumer here. The same shape as the AI service's
/// TrackingEventConsumer — kept in one place so the two can't drift.
/// </summary>
public static class MessageRetry
{
    private const int MaxAttempts = 4;

    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);

    /// A ceiling matters more than it looks. Without one, a later attempt would
    /// wait minutes holding an unacknowledged message and a connection.
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    public static async Task ExecuteAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        ILogger logger,
        Func<Task> handle,
        CancellationToken ct
    )
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await handle();
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                return;
            }
            catch (OperationCanceledException)
            {
                // Shutting down. Leave it unacknowledged so the broker
                // redelivers to whoever starts next.
                return;
            }
            catch (JsonException ex)
            {
                // Broken for good. Retrying a malformed payload produces the
                // same result three more times and blocks everything behind it.
                logger.LogError(
                    ex,
                    "Unreadable payload on {RoutingKey}, dead-lettering without retry",
                    ea.RoutingKey
                );
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                var delay = DelayFor(attempt);

                logger.LogWarning(
                    ex,
                    "Attempt {Attempt} of {Max} failed for {RoutingKey}, retrying in {Seconds:0.0}s",
                    attempt,
                    MaxAttempts,
                    ea.RoutingKey,
                    delay.TotalSeconds
                );

                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                // requeue: false sends it to the DLQ rather than back to the head
                // of the queue, where it would fail instantly and forever.
                logger.LogError(
                    ex,
                    "{RoutingKey} failed {Max} times, dead-lettering",
                    ea.RoutingKey,
                    MaxAttempts
                );
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
                return;
            }
        }
    }

    /// <summary>
    /// Exponential backoff with equal jitter — 1s, 2s, 4s, 8s, each randomised
    /// across the upper half of its window.
    ///
    /// When a shared dependency fails, every in-flight message fails at the same
    /// instant. On fixed delays they all retry at the same instant too, and knock
    /// it over again the moment it recovers. Jitter is what turns a retry storm
    /// back into a queue.
    ///
    /// Equal rather than full jitter: half of each window is fixed, so a retry
    /// can never fire almost immediately and waste an attempt.
    /// </summary>
    private static TimeSpan DelayFor(int attempt)
    {
        var window = Math.Min(
            BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1),
            MaxDelay.TotalMilliseconds
        );

        var half = window / 2;

        // Random.Shared is thread-safe, so no lock is needed.
        return TimeSpan.FromMilliseconds(half + Random.Shared.NextDouble() * half);
    }
}
