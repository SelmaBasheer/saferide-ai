using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Ai.Application.Anomalies.Classify;
using SafeRide.Ai.Application.Anomalies.RecordDeviation;
using SafeRide.Ai.Infrastructure.Persistence;

namespace SafeRide.Ai.Infrastructure.Messaging;

public sealed class TrackingEventConsumer(
    IOptions<RabbitMqSettings> options,
    IServiceScopeFactory scopeFactory,
    ILogger<TrackingEventConsumer> logger
) : BackgroundService
{
    private const string DeadLetterExchange = "saferide.dlx";

    private const int MaxAttempts = 3;

    /// Short and increasing. Long enough for a connection blip or a database
    /// failover, short enough that one bad message cannot block the queue.
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly RabbitMqSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // The broker may not be up yet, or may have restarted. Retry rather
                // than letting an unhandled exception stop the host.
                logger.LogError(ex, "Consumer failed, retrying in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.Username,
            Password = _settings.Password,
        };

        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            _settings.Exchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        await channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            _settings.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = _settings.Queue,
            },
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            $"{_settings.Queue}.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );

        await channel.QueueBindAsync(
            $"{_settings.Queue}.dlq",
            DeadLetterExchange,
            _settings.Queue,
            cancellationToken: ct
        );

        await channel.QueueBindAsync(
            _settings.Queue,
            _settings.Exchange,
            TrackingRoutingKeys.RouteDeviationDetected,
            cancellationToken: ct
        );

        // Take a handful at a time — the LLM step later is slow, and a large
        // prefetch would leave messages sitting unacknowledged on one consumer.
        await channel.BasicQosAsync(0, 10, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => HandleAsync(channel, ea, ct);

        await channel.BasicConsumeAsync(
            _settings.Queue,
            autoAck: false,
            consumer,
            cancellationToken: ct
        );

        logger.LogInformation(
            "Listening on {Queue} bound to {Exchange}",
            _settings.Queue,
            _settings.Exchange
        );

        await Task.Delay(Timeout.Infinite, ct);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        // Reading the message is separated from acting on it, because the two
        // fail for different reasons. A message we cannot read is broken for
        // good: retrying it a hundred times produces the same result, so it goes
        // straight to the dead letter queue for a human to look at.
        RouteDeviationDetectedEvent? evt;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            evt = JsonSerializer.Deserialize<RouteDeviationDetectedEvent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Deviation event is not valid JSON, dead-lettering without retry");
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
            return;
        }

        if (evt is null || evt.TripId == Guid.Empty || evt.SchoolId == Guid.Empty)
        {
            logger.LogError(
                "Deviation event is missing required fields, dead-lettering without retry"
            );

            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
            return;
        }

        // Everything past this point can fail for reasons that pass: a database
        // failing over, a network blip. Those deserve another attempt.
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await ProcessAsync(evt, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                return;
            }
            catch (OperationCanceledException)
            {
                // Shutting down. Leave the message unacknowledged so the broker
                // redelivers it to whoever starts next.
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                var delay = RetryDelays[attempt - 1];

                logger.LogWarning(
                    ex,
                    "Attempt {Attempt} of {Max} failed for event {EventId}, retrying in {Seconds}s",
                    attempt,
                    MaxAttempts,
                    evt.EventId,
                    delay.TotalSeconds
                );

                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                // Out of attempts. requeue: false sends it to the DLQ rather than
                // back to the head of the queue, where it would fail instantly and
                // forever in a hot loop.
                logger.LogError(
                    ex,
                    "Event {EventId} failed {Max} times, dead-lettering",
                    evt.EventId,
                    MaxAttempts
                );

                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
                return;
            }
        }
    }

    /// One complete attempt. It takes a fresh scope every time on purpose: a
    /// DbContext that has thrown is not safe to reuse, so a retry must start
    /// with a clean one.
    private async Task ProcessAsync(RouteDeviationDetectedEvent evt, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var inbox = scope.ServiceProvider.GetRequiredService<InboxStore>();

        // The receipt book. A message we have already finished is dropped. This
        // is what makes replaying a batch safe: pressing the button twice cannot
        // produce two alerts.
        if (evt.EventId != Guid.Empty && await inbox.HasProcessedAsync(evt.EventId, ct))
        {
            logger.LogInformation("Event {EventId} was already processed, skipping", evt.EventId);
            return;
        }

        var record = scope.ServiceProvider.GetRequiredService<RecordDeviationHandler>();
        var classify = scope.ServiceProvider.GetRequiredService<ClassifyAnomalyHandler>();

        var recorded = await record.HandleAsync(
            new RecordDeviationCommand(
                evt.TripId,
                evt.SchoolId,
                evt.BusId,
                evt.RouteCode,
                evt.RouteName,
                evt.Latitude,
                evt.Longitude,
                evt.MetresOffRoute,
                evt.SpeedKmh,
                evt.StopsTotal,
                evt.StopsReached,
                evt.TripStartedAt,
                evt.OccurredAtUtc
            ),
            ct
        );

        // Null means a second alert would duplicate one a human is already
        // dealing with. An unfinished alert comes back with its id instead, so
        // the classification below completes it.
        if (recorded.IsSuccess && recorded.Value is { } anomalyId)
        {
            await classify.HandleAsync(anomalyId, ct);
        }

        // The receipt is signed last, and only once everything above succeeded.
        if (evt.EventId != Guid.Empty)
        {
            try
            {
                await inbox.MarkProcessedAsync(
                    evt.EventId,
                    nameof(RouteDeviationDetectedEvent),
                    ct
                );
            }
            catch (DbUpdateException)
            {
                // Another delivery of the same message signed first. Nothing is
                // wrong — the primary key did exactly its job.
                logger.LogInformation(
                    "Event {EventId} was recorded by another delivery",
                    evt.EventId
                );
            }
        }
    }
}
