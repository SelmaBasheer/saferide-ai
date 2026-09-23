namespace SafeRide.Ai.Application.Abstractions;

/// The application layer needs to announce that something happened. It does
/// not need to know that a broker exists, so the port says nothing about
/// RabbitMQ, exchanges or channels.
public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default);
}
