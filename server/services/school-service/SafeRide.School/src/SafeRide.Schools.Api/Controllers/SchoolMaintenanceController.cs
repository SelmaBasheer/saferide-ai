using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Application.Abstractions;

namespace SafeRide.Schools.Api.Controllers;

/// <summary>
/// Operational tools, kept away from the school API on purpose — nothing here
/// is part of normal use, and mixing the two would invite someone to call it.
/// </summary>
[ApiController]
[Route("api/schools/maintenance")]
public sealed class SchoolMaintenanceController(IEventReplayer replayer) : ControllerBase
{
    /// <summary>
    /// Re-publishes every school, subscription and captured payment.
    ///
    /// Safe to run more than once: dimensions are upserts guarded by a
    /// timestamp, and payment facts are keyed by the payment's own id. Running
    /// it twice must not change a single number — which is the point of saying
    /// the consumers are idempotent rather than hoping they are.
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("replay-events")]
    public async Task<IActionResult> ReplayEvents(CancellationToken ct)
    {
        var summary = await replayer.ReplayAsync(ct);
        return Ok(ApiResponse<ReplaySummary>.Ok(summary));
    }
}
