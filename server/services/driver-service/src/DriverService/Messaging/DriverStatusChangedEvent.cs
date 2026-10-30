namespace DriverService.Messaging;

/// <summary>
/// One shape, two routing keys. Consumers that care only about access
/// revocation bind to driver-deactivated; the payload carries the resulting
/// status so a consumer handling both keys needs no branching.
/// </summary>
public sealed record DriverStatusChangedEvent(
    Guid Id,
    Guid SchoolId,
    string Status,
    DateTime OccurredAt
);
