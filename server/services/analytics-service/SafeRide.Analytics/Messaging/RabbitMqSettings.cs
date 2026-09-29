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
    public string SchoolExchange { get; init; } = "school.events";
    public string SchoolQueue { get; init; } = "analytics.school-events";
    public string TrackingExchange { get; init; } = "tracking.events";
    public string TrackingQueue { get; init; } = "analytics.tracking-events";
}
