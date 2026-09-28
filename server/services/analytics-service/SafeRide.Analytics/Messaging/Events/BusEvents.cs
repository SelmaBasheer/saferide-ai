namespace SafeRide.Analytics.Messaging.Events;

/// Published by the Bus service. These are its contract, not ours — the shapes
/// mirror the Java records exactly and must not be "improved" here.
public sealed record BusCreated(
    Guid BusId,
    Guid SchoolId,
    string RegistrationNumber,
    string Model,
    int Capacity,
    DateTime OccurredAtUtc
);

public sealed record BusStatusChanged(
    Guid BusId,
    Guid SchoolId,
    string RegistrationNumber,
    bool Active,
    bool DocumentsValid,
    DateTime OccurredAtUtc
);
