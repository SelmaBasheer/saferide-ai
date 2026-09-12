using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Abstractions;

public interface IAnomalyRepository
{
    Task<bool> HasUnresolvedAsync(Guid tripId, AnomalyType type, CancellationToken ct = default);
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
