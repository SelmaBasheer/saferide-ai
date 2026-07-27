namespace SafeRide.Identity.Domain.Exceptions;

// Base for domain rule violations. Framework-free — carries an error code, not an HTTP status.
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
