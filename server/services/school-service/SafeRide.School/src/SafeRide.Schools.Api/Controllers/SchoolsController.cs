using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Api.Contracts;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Schools.Command;
using SafeRide.Schools.Application.Schools.Query;
using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Api.Controllers;

[Route("api/schools")]
[ApiController]
public class SchoolsController(
    ApproveSchoolHandler handler,
    GetSchoolsHandler query,
    GetMySchoolHandler myschool,
    GetSchoolByIdHandler schoolById,
    UpdateSchoolProfileHandler updateProfile,
    UploadSchoolDocumentHandler uploadDocument,
    SubmitSchoolHandler submit,
    GetDocumentDownloadUrlHandler downloadUrl,
    IMapper mapper
) : ControllerBase
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var reviewerUserId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!
        );
        var result = await handler.ApproveAsync(id, reviewerUserId, ct);
        return result.ToApiResponse(ResponseMessages.SchoolApproved);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        var result = await handler.SuspendAsync(id, ct);
        return result.ToApiResponse(ResponseMessages.SchoolSuspended);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SchoolStatus? status, CancellationToken ct)
    {
        var schools = await query.GetAllAsync(status, ct);
        var dtos = mapper.Map<List<SchoolDto>>(schools);
        return Ok(ApiResponse<List<SchoolDto>>.Ok(dtos));
    }

    [Authorize(Roles = "SchoolAdmin")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMySchool(CancellationToken ct)
    {
        var result = await myschool.GetAsync(User.GetUserId(), ct);
        return result.ToApiResponse(s => mapper.Map<SchoolDetailResponse>(s));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await schoolById.GetAsync(id, ct);
        return result.ToApiResponse(s => mapper.Map<SchoolDetailResponse>(s));
    }

    [Authorize(Roles = "SchoolAdmin")]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateMyProfile(
        UpdateProfileRequest request,
        CancellationToken ct
    )
    {
        var command = mapper.Map<UpdateSchoolProfileCommand>(request);
        var result = await updateProfile.UpdateAsync(User.GetUserId(), command, ct);
        return result.ToApiResponse(ResponseMessages.ProfileUpdated);
    }

    [Authorize(Roles = "SchoolAdmin")]
    [HttpPost("me/documents")]
    public async Task<IActionResult> UploadDocument(
        IFormFile file,
        [FromForm] DocumentType documentType,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(
                ApiResponse<object?>.Fail(ErrorCodes.FileTooLarge, "A non-empty file is required.")
            );

        await using var stream = file.OpenReadStream();
        var command = new UploadSchoolDocumentCommand(
            documentType,
            file.FileName,
            file.ContentType,
            file.Length,
            stream
        );
        var result = await uploadDocument.UploadAsync(User.GetUserId(), command, ct);
        return result.ToApiResponse(ResponseMessages.DocumentUploaded);
    }

    [Authorize(Roles = "SchoolAdmin")]
    [HttpPost("me/submit")]
    public async Task<IActionResult> Submit(CancellationToken ct)
    {
        var result = await submit.SubmitAsync(User.GetUserId(), ct);
        return result.ToApiResponse(ResponseMessages.SchoolSubmitted);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        RejectSchoolRequest request,
        CancellationToken ct
    )
    {
        var result = await handler.RejectAsync(id, User.GetUserId(), request.Reason, ct);
        return result.ToApiResponse(ResponseMessages.SchoolRejected);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(
        Guid id,
        Guid documentId,
        CancellationToken ct
    )
    {
        var result = await downloadUrl.GetAsync(id, documentId, ct);
        return result.ToApiResponse(u => new DownloadUrlResponse(u.ToString()));
    }
}
