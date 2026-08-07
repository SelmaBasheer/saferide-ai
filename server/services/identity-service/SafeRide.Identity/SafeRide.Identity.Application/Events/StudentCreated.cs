namespace SafeRide.Identity.Application.Events;

public sealed record StudentCreated(
    Guid StudentId,
    Guid SchoolId,
    string FirstName,
    string LastName,
    string ParentFirstName,
    string ParentLastName,
    string ParentEmail,
    string ParentPhone,
    DateTime OccurredAt
);
