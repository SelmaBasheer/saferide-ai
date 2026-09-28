using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Abstractions;

/// One interface per dimension rather than one repository for everything.
/// The trip consumer has no way to reach dim_school, and each interface stays
/// small enough that implementing it in a test is trivial.
public interface IBusDimensionRepository
{
    Task UpsertAsync(BusDimensionRow row, CancellationToken ct);
}
