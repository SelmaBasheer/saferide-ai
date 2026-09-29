using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Abstractions;

public interface ITripFactRepository
{
    /// <summary>
    /// One trip and its whole roster, written together. This is the only place
    /// in the projection where a single event touches two tables, so it is also
    /// the only one that needs a transaction: a trip with half a roster would be
    /// worse than no trip at all, because the report would quietly under-count.
    /// </summary>
    Task InsertAsync(
        TripFactRow trip,
        IReadOnlyList<AttendanceFactRow> roster,
        CancellationToken ct
    );
}
