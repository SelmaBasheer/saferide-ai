namespace SafeRide.Tracking.Infrastructure;

public sealed class MongoSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 27018;
    public string Database { get; init; } = "saferide_tracking";
    public string Username { get; init; } = "saferide";
    public string Password { get; init; } = "";
    public string AuthenticationDatabase { get; init; } = "admin";
}
