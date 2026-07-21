namespace SafeRide.Identity.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
}
