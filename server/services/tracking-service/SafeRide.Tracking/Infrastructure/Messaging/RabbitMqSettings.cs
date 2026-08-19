namespace SafeRide.Tracking.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "saferide";
    public string Password { get; init; } = "";
    public string Exchange { get; init; } = "tracking.events";
}
