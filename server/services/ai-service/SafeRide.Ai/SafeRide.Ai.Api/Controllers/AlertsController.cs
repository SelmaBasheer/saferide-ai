using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Ai.Api.Common;
using SafeRide.Ai.Api.Contracts;
using SafeRide.Ai.Application.Anomalies.ListAlerts;
using SafeRide.Ai.Application.Anomalies.ResolveAlert;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Api.Controllers;

[Route("api/alerts")]
[ApiController]
[Authorize(Policy = "SchoolAdmin")]
public class AlertsController(ListAlertsHandler listHandler, ResolveAlertHandler resolveHandler)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default
    )
    {
        AnomalyStatus? parsed = Enum.TryParse<AnomalyStatus>(status, true, out var value)
            ? value
            : null;

        var result = await listHandler.HandleAsync(
            new ListAlertsQuery(User.SchoolId(), parsed, page, pageSize),
            ct
        );

        return result.ToApiResponse(p => new PagedResponse<AlertResponse>(
            [.. p.Items.Select(ToResponse)],
            p.Total,
            p.Page,
            p.PageSize
        ));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await resolveHandler.ApproveAsync(id, User.SchoolId(), User.UserId(), ct);
        return result.ToApiResponse("Alert sent.");
    }

    [HttpPost("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct)
    {
        var result = await resolveHandler.DismissAsync(id, User.SchoolId(), User.UserId(), ct);
        return result.ToApiResponse("Alert dismissed.");
    }

    private static AlertResponse ToResponse(Domain.Entities.Anomaly a) =>
        new(
            a.Id,
            a.TripId,
            a.RouteCode,
            a.RouteName,
            a.Type.ToString(),
            a.Status.ToString(),
            a.DetectedAtUtc,
            a.Classification,
            a.Reasoning,
            a.DraftMessage,
            a.Confidence,
            a.ClassifiedByModel,
            a.ResolvedAtUtc
        );
}
