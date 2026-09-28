namespace SafeRide.Analytics.Models;

/// What the projection writes, independent of both the event it came from and
/// the table it lands in. Capacity is nullable because BusStatusChanged does
/// not carry it, and null must mean "unchanged" rather than "zero".
public sealed record BusDimensionRow(
    Guid BusId,
    Guid SchoolId,
    string RegistrationNumber,
    int? Capacity,
    bool IsDeleted,
    DateTime EventAtUtc
);
