namespace SafeRide.School.Domain.Exceptions;

// Thrown when a domain invariant is violated (e.g. an invalid value object).
// Pure: carries an error code, not an HTTP status. The API layer maps it.
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
