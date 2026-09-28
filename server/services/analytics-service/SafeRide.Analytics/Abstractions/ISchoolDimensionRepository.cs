using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Abstractions;

/// Two methods rather than one, because the two streams are independent and
/// each carries its own timestamp. Merging them would mean a late status
/// message could roll back a subscription change that arrived after it.
public interface ISchoolDimensionRepository
{
    Task UpsertStatusAsync(SchoolStatusRow row, CancellationToken ct);
    Task UpdateEntitlementAsync(SchoolEntitlementRow row, CancellationToken ct);
}
