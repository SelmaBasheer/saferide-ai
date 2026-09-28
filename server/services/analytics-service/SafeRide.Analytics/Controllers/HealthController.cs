using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Analytics.Data;

namespace SafeRide.Analytics.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class HealthController(IDbConnectionFactory factory) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health()
    {
        // Deliberately touches the database. A health check that only proves
        // the process is alive tells you nothing Docker doesn't already know.
        using var conn = factory.Create();
        var ok = await conn.ExecuteScalarAsync<int>("SELECT 1");

        return Ok(new { status = ok == 1 ? "healthy" : "degraded", service = "analytics" });
    }
}
