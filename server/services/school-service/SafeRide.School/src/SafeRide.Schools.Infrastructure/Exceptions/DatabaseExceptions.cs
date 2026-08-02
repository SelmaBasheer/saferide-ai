namespace SafeRide.Schools.Infrastructure.Exceptions;

// A unique constraint was violated at the database level.
public sealed class DuplicateEntityException(string message, Exception inner)
    : Exception(message, inner);

// An optimistic-concurrency conflict occurred on save.
public sealed class ConcurrencyConflictException(string message, Exception inner)
    : Exception(message, inner);
