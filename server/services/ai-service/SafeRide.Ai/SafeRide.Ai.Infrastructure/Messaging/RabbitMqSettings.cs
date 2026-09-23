namespace SafeRide.Ai.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "saferide";
    public string Password { get; set; } = string.Empty;

    /// What this service listens to.
    public string Exchange { get; set; } = "tracking.events";
    public string Queue { get; set; } = "ai.tracking-events";

    /// What this service announces on. Separate from Exchange above, because
    /// consuming someone else's events and publishing your own are different
    /// jobs that happen to use the same broker.
    public string PublishExchange { get; set; } = "ai.events";
}
