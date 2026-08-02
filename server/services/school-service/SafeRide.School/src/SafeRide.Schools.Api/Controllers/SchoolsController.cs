using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Api.Contracts;
using SafeRide.Schools.Application.Schools.Command;

namespace SafeRide.Schools.Api.Controllers;

[Route("api/schools")]
[ApiController]
public class SchoolsController(
    ApproveSchoolHandler handler,
    GetSchoolsHandler query,
    IMapper mapper
) : ControllerBase
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await handler.ApproveAsync(id, ct);
        return result.ToApiResponse(ResponseMessages.SchoolApproved);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        var result = await handler.SuspendAsync(id, ct);
        return result.ToApiResponse(ResponseMessages.SchoolSuspended);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var schools = await query.GetAllAsync(ct);
        var dtos = mapper.Map<List<SchoolDto>>(schools);
        return Ok(ApiResponse<List<SchoolDto>>.Ok(dtos));
    }
}
