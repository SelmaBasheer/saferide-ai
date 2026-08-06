namespace DriverService.Features.CreateDriver;

public record DriverCreatedEvent(
    Guid DriverId,
    Guid SchoolId,
    string FirstName,
    string LastName,
    string Email,
    DateTime OccurredAt
);
