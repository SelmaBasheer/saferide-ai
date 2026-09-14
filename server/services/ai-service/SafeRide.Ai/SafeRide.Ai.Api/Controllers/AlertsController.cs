using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Ai.Api.Common;
using SafeRide.Ai.Api.Contracts;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Application.Anomalies.ListAlerts;
using SafeRide.Ai.Application.Anomalies.ResolveAlert;
using SafeRide.Ai.Application.Anomalies.RouteReport;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Api.Controllers;

[Route("api/alerts")]
[ApiController]
[Authorize(Policy = "SchoolAdmin")]
public class AlertsController(
    ListAlertsHandler listHandler,
    ResolveAlertHandler resolveHandler,
    RouteReportHandler reportHandler
) : ControllerBase
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

    /// Backed by a stored procedure rather than by EF, so that EXECUTE on the
    /// procedure and SELECT on the table can be granted separately.
    [HttpGet("report")]
    public async Task<IActionResult> Report(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default
    )
    {
        var lines = await reportHandler.HandleAsync(User.SchoolId(), from, to, ct);

        return Ok(ApiResponse<IReadOnlyList<RouteAnomalyReportLine>>.Ok(lines, null));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await resolveHandler.ApproveAsync(id, User.SchoolId(), User.UserId(), ct);
        return result.ToApiResponse("Alert approved.");
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
