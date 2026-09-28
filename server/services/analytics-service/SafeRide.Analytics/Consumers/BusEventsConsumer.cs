using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Messaging;
using SafeRide.Analytics.Messaging.Events;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Consumers;

public sealed class BusEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<BusEventsConsumer> logger
) : BackgroundService
{
    // The Bus service is Java and publishes camelCase. Without the
    // case-insensitive flag every property deserialises to its default and the
    // rows come out empty — silently, because System.Text.Json is
    // case-sensitive by default.
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

        // Declared here too, and identically. Declaration is idempotent, and it
        // lets Analytics start before the Bus service has ever run.
        await _channel.ExchangeDeclareAsync(
            s.BusExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );

        await RabbitDLQTopology.DeclareQueueWithDlqAsync(_channel, s.BusQueue, stoppingToken);

        string[] keys = [MessagingConstants.BusCreatedKey, MessagingConstants.BusStatusChangedKey];

        foreach (var key in keys)
        {
            await _channel.QueueBindAsync(
                s.BusQueue,
                s.BusExchange,
                key,
                cancellationToken: stoppingToken
            );
        }

        // One message at a time. The ordering guard makes concurrent delivery
        // safe in principle, but two inserts for the same new bus would race on
        // the primary key, and there is nothing to gain at this volume.
        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await HandleAsync(ea.RoutingKey, json, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                // Dead-letter rather than requeue. A message this consumer cannot
                // parse will not parse the second time either, and requeuing it
                // spins forever while blocking everything behind it.
                logger.LogError(ex, "Failed to project bus event {RoutingKey}", ea.RoutingKey);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            s.BusQueue,
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleAsync(string routingKey, string json, CancellationToken ct)
    {
        BusDimensionRow? row = routingKey switch
        {
            MessagingConstants.BusCreatedKey => ToRow(
                JsonSerializer.Deserialize<BusCreated>(json, Json)!
            ),
            MessagingConstants.BusStatusChangedKey => ToRow(
                JsonSerializer.Deserialize<BusStatusChanged>(json, Json)!
            ),
            _ => null,
        };

        if (row is null)
        {
            logger.LogWarning("Ignoring unexpected routing key {RoutingKey}", routingKey);
            return;
        }

        // A default timestamp means a property didn't bind — almost always a
        // field-name mismatch with the publishing service. Fail with something
        // that says so, rather than letting SQL complain about the year 1.
        if (row.EventAtUtc == default)
        {
            throw new InvalidOperationException(
                $"Event timestamp did not deserialise for {routingKey}. Payload: {json}"
            );
        }

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBusDimensionRepository>();
        await repository.UpsertAsync(row, ct);

        logger.LogInformation("Projected {RoutingKey} for bus {BusId}", routingKey, row.BusId);
    }

    private static BusDimensionRow ToRow(BusCreated e) =>
        new(
            e.BusId,
            e.SchoolId,
            e.RegistrationNumber,
            e.Capacity,
            IsDeleted: false,
            e.OccurredAtUtc
        );

    // A deactivated bus is out of service, not erased. The row has to survive so
    // September's trips keep their registration number.
    private static BusDimensionRow ToRow(BusStatusChanged e) =>
        new(
            e.BusId,
            e.SchoolId,
            e.RegistrationNumber,
            Capacity: null,
            IsDeleted: !e.Active,
            e.OccurredAtUtc
        );

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
