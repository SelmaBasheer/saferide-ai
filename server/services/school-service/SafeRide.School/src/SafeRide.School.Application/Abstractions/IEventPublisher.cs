namespace SafeRide.School.Application.Abstractions;

// Port for publishing integration events. Implemented in Infrastructure (RabbitMQ).
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        string exchange,
        string routingKey,
        TEvent message,
        CancellationToken ct = default
    )
        where TEvent : class;
}
