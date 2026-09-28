namespace SafeRide.Analytics.Messaging;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; init; } = "localhost";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";

    // Exchange and queue names live in config rather than in code, because the
    // exchange is owned by another service and its name is its contract.
    public string BusExchange { get; init; } = "bus.events";
    public string BusQueue { get; init; } = "analytics.bus-events";
}
