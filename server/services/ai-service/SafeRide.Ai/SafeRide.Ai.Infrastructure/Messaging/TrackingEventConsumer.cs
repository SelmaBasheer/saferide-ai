using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Ai.Application.Anomalies.Classify;
using SafeRide.Ai.Application.Anomalies.RecordDeviation;

namespace SafeRide.Ai.Infrastructure.Messaging;

public sealed class TrackingEventConsumer(
    IOptions<RabbitMqSettings> options,
    IServiceScopeFactory scopeFactory,
    ILogger<TrackingEventConsumer> logger
) : BackgroundService
{
    private const string DeadLetterExchange = "saferide.dlx";

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
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<RouteDeviationDetectedEvent>(json, JsonOptions);

            if (evt is null)
            {
                logger.LogWarning("Received an unreadable deviation event, dead-lettering");
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
                return;
            }

            using var scope = scopeFactory.CreateScope();

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

            // Null means it was a duplicate of an unresolved alert — nothing to classify.
            if (recorded.IsSuccess && recorded.Value is { } anomalyId)
            {
                await classify.HandleAsync(anomalyId, ct);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            // requeue: false sends it to the DLQ rather than looping forever on a
            // message that will fail again. Same rule as the Java services.
            logger.LogError(ex, "Failed to handle deviation event, dead-lettering");
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
        }
    }
}
