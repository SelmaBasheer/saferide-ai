using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Ai.Api.Common;
using SafeRide.Ai.Api.Contracts;
using SafeRide.Ai.Application.Abstractions;

namespace SafeRide.Ai.Api.Controllers;

/// SuperAdmin, and the reason is tenant isolation rather than seniority: the
/// dead letter queue holds messages belonging to every school, so no SchoolAdmin
/// can be allowed near it. There is no handler behind these endpoints because
/// there is no business rule to apply — only an operator choosing to look, or
/// to retry.
[Route("api/maintenance")]
[ApiController]
[Authorize(Policy = "SuperAdmin")]
public class MaintenanceController(IDeadLetterQueue deadLetters) : ControllerBase
{
    private const int MaxBatch = 50;

    [HttpGet("dlq")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var status = await deadLetters.GetStatusAsync(ct);
        return Ok(ApiResponse<DeadLetterStatus>.Ok(status, null));
    }

    [HttpGet("dlq/messages")]
    public async Task<IActionResult> Peek([FromQuery] int max = 10, CancellationToken ct = default)
    {
        var messages = await deadLetters.PeekAsync(Math.Clamp(max, 1, MaxBatch), ct);
        return Ok(ApiResponse<IReadOnlyList<DeadLetterMessage>>.Ok(messages, null));
    }

    [HttpPost("dlq/replay")]
    public async Task<IActionResult> Replay(
        [FromQuery] int max = 10,
        CancellationToken ct = default
    )
    {
        var replayed = await deadLetters.ReplayAsync(Math.Clamp(max, 1, MaxBatch), ct);
        return Ok(ApiResponse<object?>.Ok(new { replayed }, $"Replayed {replayed} message(s)."));
    }
}
