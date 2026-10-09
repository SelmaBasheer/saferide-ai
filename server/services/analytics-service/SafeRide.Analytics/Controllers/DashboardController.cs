using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Common;
using SafeRide.Analytics.Services;

namespace SafeRide.Analytics.Controllers;

[ApiController]
[Route("api/analytics/dashboard")]
public sealed class DashboardController(
    ISuperAdminDashboardRepository dashboard,
    LocalDates localDates
) : ControllerBase
{
    /// <summary>
    /// Platform health at a glance.
    ///
    /// The range filters things that happened — onboardings, purchases, the
    /// revenue trend. It deliberately does not filter current state: how many
    /// schools are suspended is true today, not "between two dates". Defaults
    /// to the last twelve months so the page says something before anyone
    /// touches the filter.
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("super-admin")]
    public async Task<IActionResult> SuperAdmin(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct = default
    )
    {
        var range = ReportRanges.ResolveDashboard(
            from,
            to,
            localDates.ToLocalDate(DateTime.UtcNow)
        );
        var result = await dashboard.BuildAsync(range, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }
}
