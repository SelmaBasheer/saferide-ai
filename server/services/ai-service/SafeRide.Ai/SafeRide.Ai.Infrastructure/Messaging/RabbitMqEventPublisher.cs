using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SafeRide.Ai.Application.Abstractions;

namespace SafeRide.Ai.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(
    IOptions<RabbitMqSettings> options,
    ILogger<RabbitMqEventPublisher> logger
) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RabbitMqSettings _settings = options.Value;

    public async Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.Username,
            Password = _settings.Password,
        };

        await using var connection = await factory.CreateConnectionAsync(ct);

        // Publisher confirms: the call below does not complete until the broker
        // says it has the message. Without this, publishing is fire and hope —
        // the method returns successfully whether or not anything arrived.
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ),
            cancellationToken: ct
        );

        await channel.ExchangeDeclareAsync(
            _settings.PublishExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));

        var properties = new BasicProperties
        {
            // Required. The Notification service uses a JSON message converter,
            // and without this header Spring treats the body as raw bytes and
            // the listener never fires.
            ContentType = "application/json",

            // 2 means the broker writes it to disk, so a broker restart between
            // publish and delivery does not lose it.
            DeliveryMode = DeliveryModes.Persistent,
        };

        await channel.BasicPublishAsync(
            _settings.PublishExchange,
            routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct
        );

        logger.LogInformation(
            "Published {RoutingKey} to {Exchange}",
            routingKey,
            _settings.PublishExchange
        );
    }
}
