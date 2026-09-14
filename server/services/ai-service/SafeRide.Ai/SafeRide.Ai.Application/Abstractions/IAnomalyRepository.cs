using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Abstractions;

public interface IAnomalyRepository
{
    /// Returns the existing unresolved anomaly rather than just "yes/no", because
    /// the caller needs to know *which* one — an unfinished alert has to be
    /// finished, not skipped.
    Task<Anomaly?> GetUnresolvedAsync(
        Guid tripId,
        AnomalyType type,
        CancellationToken ct = default
    );
    Task AddAsync(Anomaly anomaly, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<Anomaly?> GetAsync(Guid id, CancellationToken ct = default);
    Task<(List<Anomaly> Items, long Total)> ListAsync(
        Guid schoolId,
        AnomalyStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}
