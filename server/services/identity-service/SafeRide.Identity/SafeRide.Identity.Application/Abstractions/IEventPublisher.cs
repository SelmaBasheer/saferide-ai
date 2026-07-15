namespace SafeRide.Identity.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        string exchange,
        string routingKey,
        TEvent @event,
        CancellationToken ct = default
    )
        where TEvent : class;
}
