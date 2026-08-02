namespace SafeRide.Identity.Infrastructure.Exceptions;

public sealed class InfrastructureException(string message, Exception? inner = null)
    : Exception(message, inner);
