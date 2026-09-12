using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Application.Common;
using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Anomalies.ListAlerts;

public sealed record ListAlertsQuery(Guid SchoolId, AnomalyStatus? Status, int Page, int PageSize);

public sealed record AlertPage(List<Anomaly> Items, long Total, int Page, int PageSize);

public sealed class ListAlertsHandler(IAnomalyRepository anomalies)
{
    private const int MaxPageSize = 50;

    public async Task<Result<AlertPage>> HandleAsync(ListAlertsQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var size = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var (items, total) = await anomalies.ListAsync(
            query.SchoolId,
            query.Status,
            page,
            size,
            ct
        );

        return Result.Success(new AlertPage(items, total, page, size));
    }
}
