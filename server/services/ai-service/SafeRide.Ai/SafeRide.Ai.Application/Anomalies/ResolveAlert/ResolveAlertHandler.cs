using Microsoft.Extensions.Logging;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Application.Common;

namespace SafeRide.Ai.Application.Anomalies.ResolveAlert;

public sealed class ResolveAlertHandler(
    IAnomalyRepository anomalies,
    ILogger<ResolveAlertHandler> logger
)
{
    public async Task<Result> ApproveAsync(
        Guid alertId,
        Guid schoolId,
        Guid userId,
        CancellationToken ct
    ) => await ResolveAsync(alertId, schoolId, userId, approve: true, ct);

    public async Task<Result> DismissAsync(
        Guid alertId,
        Guid schoolId,
        Guid userId,
        CancellationToken ct
    ) => await ResolveAsync(alertId, schoolId, userId, approve: false, ct);

    private async Task<Result> ResolveAsync(
        Guid alertId,
        Guid schoolId,
        Guid userId,
        bool approve,
        CancellationToken ct
    )
    {
        var anomaly = await anomalies.GetAsync(alertId, ct);

        // Another school's alert is Not Found, not Forbidden — a 403 would confirm
        // that the record exists.
        if (anomaly is null || anomaly.SchoolId != schoolId)
        {
            return Result.Failure(AnomalyErrors.NotFound);
        }

        var resolved = approve ? anomaly.TryApprove(userId) : anomaly.TryDismiss(userId);

        if (!resolved)
        {
            return Result.Failure(AnomalyErrors.AlreadyResolved);
        }

        await anomalies.SaveChangesAsync(ct);

        logger.LogInformation(
            "Alert {AlertId} {Action} by {UserId}",
            alertId,
            approve ? "approved" : "dismissed",
            userId
        );

        return Result.Success();
    }
}
