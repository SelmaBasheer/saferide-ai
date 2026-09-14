using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using SafeRide.Ai.Application.Abstractions;

namespace SafeRide.Ai.Infrastructure.Messaging;

public sealed class RabbitMqDeadLetterStore(
    IOptions<RabbitMqSettings> options,
    ILogger<RabbitMqDeadLetterStore> logger
) : IDeadLetterStore
{
    private readonly RabbitMqSettings _settings = options.Value;

    private string QueueName => $"{_settings.Queue}.dlq";

    public async Task<DeadLetterStatus> GetStatusAsync(CancellationToken ct = default)
    {
        await using var connection = await ConnectAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        try
        {
            // Passive: asking "how many?" must not be able to create the queue.
            var declared = await channel.QueueDeclarePassiveAsync(QueueName, ct);

            return new DeadLetterStatus(QueueName, declared.MessageCount);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
        {
            // The consumer declares the topology at startup, so an operator who
            // asks first gets a 404. That is not a fault: nothing is parked
            // because the queue does not exist yet. Any other broker error is a
            // real problem and is allowed through.
            logger.LogInformation("{Queue} does not exist yet, reporting empty", QueueName);

            return new DeadLetterStatus(QueueName, 0);
        }
    }

    public async Task<IReadOnlyList<DeadLetterMessage>> PeekAsync(
        int max,
        CancellationToken ct = default
    )
    {
        await using var connection = await ConnectAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        var found = new List<DeadLetterMessage>();
        var held = new List<ulong>();

        // Hold every delivery unacknowledged while reading, then return them all
        // at the end. Nacking inside the loop would put a message back at the
        // head of the queue and we would read the same one over and over.
        for (var i = 0; i < max; i++)
        {
            var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, ct);

            if (delivery is null)
            {
                break;
            }

            found.Add(Describe(delivery));
            held.Add(delivery.DeliveryTag);
        }

        foreach (var tag in held)
        {
            await channel.BasicNackAsync(tag, multiple: false, requeue: true, ct);
        }

        return found;
    }

    public async Task<int> ReplayAsync(int max, CancellationToken ct = default)
    {
        await using var connection = await ConnectAsync(ct);

        // Publisher confirms: BasicPublishAsync waits for the broker to accept
        // the message, so the ack below can never discard something the broker
        // never actually took.
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ),
            cancellationToken: ct
        );

        var replayed = 0;

        for (var i = 0; i < max; i++)
        {
            var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, ct);

            if (delivery is null)
            {
                break;
            }

            var described = Describe(delivery);

            try
            {
                // Fresh properties on purpose: the replayed message re-enters as a
                // new attempt rather than carrying its old x-death history, so the
                // retry count starts from zero. The inbox is what stops it being
                // processed twice, not the header.
                await channel.BasicPublishAsync(
                    _settings.Exchange,
                    described.OriginalRoutingKey,
                    mandatory: true,
                    new BasicProperties { Persistent = true, ContentType = "application/json" },
                    delivery.Body.ToArray(),
                    ct
                );

                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
                replayed++;
            }
            catch (Exception ex)
            {
                // Leave it where it is and stop. Something is wrong with the
                // exchange, and emptying the DLQ into a void would be worse than
                // leaving the messages parked.
                logger.LogError(
                    ex,
                    "Could not republish a dead-lettered message, leaving it in {Queue}",
                    QueueName
                );

                await channel.BasicNackAsync(
                    delivery.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    ct
                );

                break;
            }
        }

        // Warning, not Information: a human deliberately replaying failures is
        // worth being able to find in the logs afterwards.
        logger.LogWarning("Replayed {Count} messages from {Queue}", replayed, QueueName);

        return replayed;
    }

    private Task<IConnection> ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.Username,
            Password = _settings.Password,
        };

        return factory.CreateConnectionAsync(ct);
    }

    /// RabbitMQ records why a message died in an x-death header: the reason, how
    /// many times it has died, and the routing key it was originally published
    /// with — which is what a replay has to publish it back under.
    private static DeadLetterMessage Describe(BasicGetResult delivery)
    {
        var body = Encoding.UTF8.GetString(delivery.Body.ToArray());
        var routingKey = TrackingRoutingKeys.RouteDeviationDetected;
        var reason = "unknown";
        var count = 0L;

        if (
            delivery.BasicProperties.Headers is { } headers
            && headers.TryGetValue("x-death", out var raw)
            && raw is IList<object> deaths
            && deaths.Count > 0
            && deaths[0] is IDictionary<string, object?> first
        )
        {
            if (first.TryGetValue("reason", out var rawReason))
            {
                reason = AsText(rawReason) ?? reason;
            }

            if (first.TryGetValue("count", out var rawCount) && rawCount is long parsed)
            {
                count = parsed;
            }

            if (
                first.TryGetValue("routing-keys", out var rawKeys)
                && rawKeys is IList<object> keys
                && keys.Count > 0
            )
            {
                routingKey = AsText(keys[0]) ?? routingKey;
            }
        }

        return new DeadLetterMessage(routingKey, reason, count, body);
    }

    /// RabbitMQ hands string header values back as byte arrays.
    private static string? AsText(object? value) =>
        value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => null,
        };
}
