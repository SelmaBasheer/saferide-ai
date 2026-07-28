namespace SafeRide.Schools.Infrastructure.Exceptions;

// Thrown when an external dependency fails (broker, DB, HTTP call, etc.).
public sealed class InfrastructureException(string message) : Exception(message);
