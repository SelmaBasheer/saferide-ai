using Microsoft.Extensions.Logging;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Application.Common;

namespace SafeRide.Ai.Application.Anomalies.ResolveAlert;

public sealed class ResolveAlertHandler(
    IAnomalyRepository anomalies,
    IEventPublisher events,
    ILogger<ResolveAlertHandler> logger
)
{
    public async Task<Result> ApproveAsync(
        Guid alertId,
        Guid schoolId,
        Guid userId,
        string recipientEmail,
        CancellationToken ct
    ) => await ResolveAsync(alertId, schoolId, userId, recipientEmail, approve: true, ct);

    public async Task<Result> DismissAsync(
        Guid alertId,
        Guid schoolId,
        Guid userId,
        CancellationToken ct
    ) => await ResolveAsync(alertId, schoolId, userId, recipientEmail: null, approve: false, ct);

    private async Task<Result> ResolveAsync(
        Guid alertId,
        Guid schoolId,
        Guid userId,
        string? recipientEmail,
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

        if (approve)
        {
            await AnnounceAsync(anomaly, recipientEmail, ct);
        }

        return Result.Success();
    }

    private async Task AnnounceAsync(
        Domain.Entities.Anomaly anomaly,
        string? recipientEmail,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            logger.LogWarning(
                "Alert {AlertId} approved but the token carried no email claim, so nothing was sent",
                anomaly.Id
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(anomaly.DraftMessage))
        {
            logger.LogWarning(
                "Alert {AlertId} approved but has no draft message, so nothing was sent",
                anomaly.Id
            );
            return;
        }

        try
        {
            await events.PublishAsync(
                AlertApprovedEvent.RoutingKey,
                new AlertApprovedEvent(
                    Guid.NewGuid(),
                    anomaly.SchoolId,
                    anomaly.Id,
                    anomaly.RouteCode,
                    anomaly.RouteName,
                    anomaly.Type.ToString(),
                    anomaly.DraftMessage,
                    recipientEmail,
                    DateTime.UtcNow
                ),
                ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Alert {AlertId} was approved but the notification could not be published",
                anomaly.Id
            );
        }
    }
}
