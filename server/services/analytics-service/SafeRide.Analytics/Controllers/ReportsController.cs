using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Analytics.Common;
using SafeRide.Analytics.Services;

namespace SafeRide.Analytics.Controllers;

[ApiController]
[Route("api/analytics/reports")]
public sealed class ReportsController(SuperAdminReportService superAdmin) : ControllerBase
{
    /// <summary>
    /// Schools, revenue and plans for a date range. Deliberately carries no
    /// student data: a super admin runs the platform, not a school, and tenant
    /// isolation applies upward as well as sideways.
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("super-admin")]
    public async Task<IActionResult> SuperAdmin(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string format = "json",
        CancellationToken ct = default
    )
    {
        var range = superAdmin.ResolveRange(from, to);
        var report = await superAdmin.BuildAsync(range, ct);

        // JSON for the screen, CSV for the download — the same report either
        // way, so what the user sees and what they save cannot disagree.
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(ApiResponse<object>.Ok(report));
        }

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = SuperAdminReportService.ToCsv(report);
            return File(bytes, "text/csv", FileName(range, "csv"));
        }

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdf = SuperAdminReportPdf.Render(report);
            return File(pdf, "application/pdf", FileName(range, "pdf"));
        }

        throw AppException.BadRequest(
            "Report.UnknownFormat",
            "Supported formats are json, csv and pdf."
        );
    }

    private static string FileName(Models.ReportRange range, string extension) =>
        $"saferide-platform-{range.From:yyyy-MM-dd}-to-{range.To:yyyy-MM-dd}.{extension}";
}
