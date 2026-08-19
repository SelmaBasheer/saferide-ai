namespace SafeRide.Tracking.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, object payload, CancellationToken ct = default);
}
