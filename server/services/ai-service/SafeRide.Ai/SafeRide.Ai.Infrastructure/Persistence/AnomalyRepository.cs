using Microsoft.EntityFrameworkCore;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Infrastructure.Persistence;

public sealed class AnomalyRepository(AiDbContext context) : IAnomalyRepository
{
    public Task<bool> HasUnresolvedAsync(
        Guid tripId,
        AnomalyType type,
        CancellationToken ct = default
    ) =>
        context.Anomalies.AnyAsync(
            a =>
                a.TripId == tripId
                && a.Type == type
                && (a.Status == AnomalyStatus.Detected || a.Status == AnomalyStatus.Classified),
            ct
        );

    public async Task AddAsync(Anomaly anomaly, CancellationToken ct = default) =>
        await context.Anomalies.AddAsync(anomaly, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

    public Task<Anomaly?> GetAsync(Guid id, CancellationToken ct = default) =>
        context.Anomalies.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(List<Anomaly> Items, long Total)> ListAsync(
        Guid schoolId,
        AnomalyStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Anomalies.Where(a => a.SchoolId == schoolId);

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.DetectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
