namespace SafeRide.Analytics.Abstractions;

/// The school admin's own school, always from the JWT claim. A schoolId in a
/// query string would let one school read another's attendance.
public interface ITenantProvider
{
    Guid? SchoolId { get; }
}
