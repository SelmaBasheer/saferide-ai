using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Ai.Application.Anomalies.Classify;
using SafeRide.Ai.Application.Anomalies.RecordAnomaly;
using SafeRide.Ai.Domain.Enums;
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

    /// Every routing key this service knows how to turn into an anomaly. Adding a
    /// detector means adding a key here and a case in Parse — nothing else in the
    /// consumer changes.
    private static readonly string[] RoutingKeys =
    [
        TrackingRoutingKeys.RouteDeviationDetected,
        TrackingRoutingKeys.StopsSkipped,
    ];

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

        foreach (var key in RoutingKeys)
        {
            await channel.QueueBindAsync(
                _settings.Queue,
                _settings.Exchange,
                key,
                cancellationToken: ct
            );
        }

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
            "Listening on {Queue} bound to {Exchange} for {Keys}",
            _settings.Queue,
            _settings.Exchange,
            string.Join(", ", RoutingKeys)
        );

        await Task.Delay(Timeout.Infinite, ct);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        // Reading the message is separated from acting on it, because the two
        // fail for different reasons. A message we cannot read is broken for
        // good: retrying it a hundred times produces the same result, so it goes
        // straight to the dead letter queue for a human to look at.
        ParsedEvent? parsed;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            parsed = Parse(ea.RoutingKey, json);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Event is not valid JSON, dead-lettering without retry");
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
            return;
        }

        if (parsed is null)
        {
            logger.LogError(
                "Event on {RoutingKey} is unreadable or missing required fields, dead-lettering without retry",
                ea.RoutingKey
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
                await ProcessAsync(parsed, ct);
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
                    parsed.EventId,
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
                    parsed.EventId,
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
    private async Task ProcessAsync(ParsedEvent parsed, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var inbox = scope.ServiceProvider.GetRequiredService<InboxStore>();

        // The receipt book. A message we have already finished is dropped. This
        // is what makes replaying a batch safe: pressing the button twice cannot
        // produce two alerts.
        if (await inbox.HasProcessedAsync(parsed.EventId, ct))
        {
            logger.LogInformation(
                "Event {EventId} was already processed, skipping",
                parsed.EventId
            );
            return;
        }

        var record = scope.ServiceProvider.GetRequiredService<RecordAnomalyHandler>();
        var classify = scope.ServiceProvider.GetRequiredService<ClassifyAnomalyHandler>();

        var recorded = await record.HandleAsync(parsed.Command, ct);

        // Null means a second alert would duplicate one a human is already
        // dealing with. An unfinished alert comes back with its id instead, so
        // the classification below completes it.
        if (recorded.IsSuccess && recorded.Value is { } anomalyId)
        {
            await classify.HandleAsync(anomalyId, ct);
        }

        // The receipt is signed last, and only once everything above succeeded.
        try
        {
            await inbox.MarkProcessedAsync(parsed.EventId, parsed.EventType, ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Another delivery of the same message signed first. Nothing is
            // wrong — the primary key did exactly its job. Any *other* write
            // failure must not be swallowed: acknowledging then would leave the
            // side effects in place with no receipt, and a replay would redo them.
            logger.LogInformation(
                "Event {EventId} was recorded by another delivery",
                parsed.EventId
            );
        }
    }

    /// Turns whatever arrived into the one shape the application layer works in.
    /// The raw JSON is carried through untouched as the context the model reads,
    /// so there is no translation layer here to drift out of date.
    private static ParsedEvent? Parse(string routingKey, string json)
    {
        switch (routingKey)
        {
            case TrackingRoutingKeys.RouteDeviationDetected:
            {
                var evt = JsonSerializer.Deserialize<RouteDeviationDetectedEvent>(
                    json,
                    JsonOptions
                );

                if (evt is null || !IsUsable(evt.EventId, evt.TripId, evt.SchoolId))
                {
                    return null;
                }

                return new ParsedEvent(
                    evt.EventId,
                    nameof(RouteDeviationDetectedEvent),
                    new RecordAnomalyCommand(
                        evt.SchoolId,
                        evt.TripId,
                        evt.BusId,
                        evt.RouteCode,
                        evt.RouteName,
                        AnomalyType.RouteDeviation,
                        json,
                        evt.OccurredAtUtc
                    )
                );
            }

            case TrackingRoutingKeys.StopsSkipped:
            {
                var evt = JsonSerializer.Deserialize<StopsSkippedEvent>(json, JsonOptions);

                if (evt is null || !IsUsable(evt.EventId, evt.TripId, evt.SchoolId))
                {
                    return null;
                }

                return new ParsedEvent(
                    evt.EventId,
                    nameof(StopsSkippedEvent),
                    new RecordAnomalyCommand(
                        evt.SchoolId,
                        evt.TripId,
                        evt.BusId,
                        evt.RouteCode,
                        evt.RouteName,
                        AnomalyType.SkippedStop,
                        json,
                        evt.OccurredAtUtc
                    )
                );
            }

            default:
                return null;
        }
    }

    /// EventId is required, not optional. Without one there is no receipt and no
    /// duplicate check, so the message would quietly lose every guarantee the
    /// inbox exists to provide.
    private static bool IsUsable(Guid eventId, Guid tripId, Guid schoolId) =>
        eventId != Guid.Empty && tripId != Guid.Empty && schoolId != Guid.Empty;

    /// 2627 is a primary key violation, 2601 a unique index violation. Only these
    /// two mean "someone else got there first".
    private static bool IsDuplicateKey(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    private sealed record ParsedEvent(Guid EventId, string EventType, RecordAnomalyCommand Command);
}
