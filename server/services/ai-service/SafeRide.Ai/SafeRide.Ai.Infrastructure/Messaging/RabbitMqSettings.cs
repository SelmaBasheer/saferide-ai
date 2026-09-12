namespace SafeRide.Ai.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "saferide";
    public string Password { get; set; } = string.Empty;
    public string Exchange { get; set; } = "tracking.events";
    public string Queue { get; set; } = "ai.tracking-events";
}
