using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Messaging;
using SafeRide.Analytics.Messaging.Events;
using SafeRide.Analytics.Models;
using SafeRide.Analytics.Services;

namespace SafeRide.Analytics.Consumers;

public sealed class TrackingEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    LocalDates localDates,
    ILogger<TrackingEventsConsumer> logger
) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new UtcDateTimeConverter() },
    };

    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var s = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = s.Host,
            UserName = s.Username,
            Password = s.Password,
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            s.TrackingExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );

        await RabbitDLQTopology.DeclareQueueWithDlqAsync(_channel, s.TrackingQueue, stoppingToken);

        await _channel.QueueBindAsync(
            s.TrackingQueue,
            s.TrackingExchange,
            MessagingConstants.TripEndedKey,
            cancellationToken: stoppingToken
        );

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            await MessageRetry.ExecuteAsync(
                _channel!,
                ea,
                logger,
                () => HandleAsync(ea.RoutingKey, json, stoppingToken),
                stoppingToken
            );
        };

        await _channel.BasicConsumeAsync(
            s.TrackingQueue,
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleAsync(string routingKey, string json, CancellationToken ct)
    {
        if (routingKey != MessagingConstants.TripEndedKey)
        {
            logger.LogWarning("Ignoring unexpected routing key {RoutingKey}", routingKey);
            return;
        }

        var e =
            JsonSerializer.Deserialize<TripEnded>(json, Json)
            ?? throw new JsonException("Empty trip-ended payload.");

        if (e.StartedAt == default || e.TripId == Guid.Empty)
        {
            throw new JsonException($"trip-ended did not deserialise. Payload: {json}");
        }

        // The trip belongs to the day it started, in the school's own calendar.
        // A trip that begins at 07:00 and runs past midnight UTC is still that
        // morning's trip.
        var tripDate = localDates.ToLocalDate(e.StartedAt);

        var trip = new TripFactRow(
            e.TripId,
            e.SchoolId,
            e.RouteId,
            e.BusId,
            e.DriverId,
            e.RouteCode,
            e.RouteName,
            // Tracking stores the driver's id, not their name, and fetching it
            // would mean a cross-service call at trip start. Left null rather
            // than faked.
            DriverName: null,
            tripDate,
            e.StartedAt,
            e.EndedAt,
            e.StudentCount,
            e.BoardedCount,
            e.AbsentCount,
            e.UnmarkedCount
        );

        var roster = e
            .Roster.Select(r => new AttendanceFactRow(
                e.TripId,
                r.StudentId,
                e.SchoolId,
                tripDate,
                r.Name,
                r.PickupStopId,
                r.StopName,
                e.RouteCode,
                e.RouteName,
                e.BusId,
                r.BoardingStatus,
                r.MarkedAt
            ))
            .ToList();

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITripFactRepository>();
        await repository.InsertAsync(trip, roster, ct);

        logger.LogInformation(
            "Projected trip-ended for trip {TripId} with {Count} roster entries",
            e.TripId,
            roster.Count
        );
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
