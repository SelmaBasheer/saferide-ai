using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Api.Contracts;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Plans.Command;
using SafeRide.Schools.Application.Plans.Query;
using SafeRide.Schools.Domain.Entities;

namespace SafeRide.Schools.Api.Controllers;

[Route("api/plans")]
[ApiController]
public class PlansController(CreatePlanHandler createPlan, GetPlansHandler getPlans)
    : ControllerBase
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePlanRequest request, CancellationToken ct)
    {
        var result = await createPlan.CreateAsync(
            new CreatePlanCommand(
                request.Name,
                request.Description,
                request.PriceInPaise,
                request.BusLimit,
                request.DurationMonths
            ),
            ct
        );

        return result.ToApiResponse(id => new { id });
    }

    /// Any signed-in user can read plans, because a school admin has to see
    /// them to choose one. Only a super admin can ask for the inactive ones.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default
    )
    {
        var showInactive = includeInactive && User.IsInRole("SuperAdmin");

        var plans = await getPlans.GetAsync(showInactive, ct);

        return Ok(ApiResponse<IReadOnlyList<PlanDto>>.Ok([.. plans.Select(ToDto)]));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await createPlan.DeactivateAsync(id, ct);
        return result.ToApiResponse("Plan deactivated.");
    }

    /// Reuses CreatePlanRequest: an update sets every field, so the shape is
    /// identical and a second near-identical record would only drift.
    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        CreatePlanRequest request,
        CancellationToken ct
    )
    {
        var result = await createPlan.UpdateAsync(
            id,
            new CreatePlanCommand(
                request.Name,
                request.Description,
                request.PriceInPaise,
                request.BusLimit,
                request.DurationMonths
            ),
            ct
        );

        return result.ToApiResponse("Plan updated.");
    }

    // Mapped by hand rather than through AutoMapper: one small shape, and it
    // avoids a profile edit for something this shallow.
    private static PlanDto ToDto(SubscriptionPlan p) =>
        new(p.Id, p.Name, p.Description, p.PriceInPaise, p.BusLimit, p.DurationMonths, p.IsActive);
}
