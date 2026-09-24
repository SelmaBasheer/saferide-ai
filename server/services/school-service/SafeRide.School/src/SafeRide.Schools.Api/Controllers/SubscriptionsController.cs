using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Api.Contracts;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Subscriptions.Command;
using SafeRide.Schools.Application.Subscriptions.Query;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Api.Controllers;

[Route("api/subscriptions")]
[ApiController]
public class SubscriptionsController(
    ActivateSubscriptionHandler activate,
    GetSubscriptionsHandler query
) : ControllerBase
{
    /// Recording a payment that happened outside the system. A school admin
    /// must never be able to grant themselves a subscription.
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("activate")]
    public async Task<IActionResult> Activate(
        ActivateSubscriptionRequest request,
        CancellationToken ct
    )
    {
        var result = await activate.ActivateAsync(
            request.SchoolId,
            request.PlanId,
            request.StartsOn,
            ct
        );

        return result.ToApiResponse(id => new { id });
    }

    [Authorize(Roles = "SchoolAdmin")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await query.GetMineAsync(User.GetUserId(), ct);
        return result.ToApiResponse(s => ToDto(s, null));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    )
    {
        var (items, total) = await query.SearchAsync(status, page, pageSize, ct);

        var dtos = items.Select(i => ToDto(i.Subscription, i.SchoolName)).ToList();

        return Ok(
            ApiResponse<PagedResult<SubscriptionDto>>.Ok(
                new PagedResult<SubscriptionDto>(dtos, total, page, pageSize)
            )
        );
    }

    private static SubscriptionDto ToDto(Subscription s, string? schoolName) =>
        new(
            s.Id,
            s.SchoolId,
            schoolName,
            s.PlanName,
            s.PriceInPaise,
            s.BusLimit,
            s.StartsOn,
            s.EndsOn,
            s.GraceEndsOn,
            s.Status,
            s.DaysRemaining(DateOnly.FromDateTime(DateTime.UtcNow))
        );
}
