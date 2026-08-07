namespace SafeRide.Identity.Application.Events;

public sealed record DriverCreated(
    Guid DriverId,
    Guid SchoolId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTime OccurredAt
);
