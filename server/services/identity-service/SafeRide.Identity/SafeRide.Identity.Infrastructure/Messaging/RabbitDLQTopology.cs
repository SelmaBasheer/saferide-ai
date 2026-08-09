using RabbitMQ.Client;

namespace SafeRide.Identity.Infrastructure.Messaging;

public static class RabbitDLQTopology
{
    public const string DeadLetterExchange = "saferide.dlx";

    public static async Task DeclareQueueWithDlqAsync(
        IChannel channel,
        string queue,
        CancellationToken ct
    )
    {
        await channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            $"{queue}.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );
        await channel.QueueBindAsync(
            $"{queue}.dlq",
            DeadLetterExchange,
            queue,
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = queue,
            },
            cancellationToken: ct
        );
    }
}
