using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Common;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed class ApproveSchoolHandler(
    IGenericRepository<School> schools,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher
)
{
    public async Task<Result> ApproveAsync(Guid id, Guid reviewerUserId, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(id, ct);
        if (school is null)
            return Result.Failure(SchoolErrors.SchoolNotFound);

        if (school.Status != SchoolStatus.Submitted)
            return Result.Failure(SchoolErrors.NotSubmitted);

        school.Approve(reviewerUserId);
        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            MessagingConstants.SchoolEventsExchange, //"school.events",
            MessagingConstants.SchoolApprovedKey, //"school-approved",
            new SchoolApproved(
                school.Id,
                school.AdminUserId,
                school.Name,
                school.AdminEmail,
                DateTime.UtcNow
            ),
            ct
        );
        return Result.Success();
    }

    public async Task<Result> RejectAsync(
        Guid id,
        Guid reviewerUserId,
        string reason,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(SchoolErrors.ReasonRequired);

        var school = await schools.GetByIdAsync(id, ct);
        if (school is null)
            return Result.Failure(SchoolErrors.SchoolNotFound);
        if (school.Status != SchoolStatus.Submitted)
            return Result.Failure(SchoolErrors.NotSubmitted);

        school.Reject(reviewerUserId, reason);
        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            MessagingConstants.SchoolEventsExchange, //"school.events",
            MessagingConstants.SchoolRejectedKey, //"school-rejected",
            new SchoolRejected(
                school.Id,
                school.AdminUserId,
                school.Name,
                school.AdminEmail,
                reason,
                DateTime.UtcNow
            ),
            ct
        );
        return Result.Success();
    }

    public async Task<Result> SuspendAsync(Guid id, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(id, ct);
        if (school is null)
            return Result.Failure(new Error("School.NotFound", "School not found."));

        school.Suspend();
        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            MessagingConstants.SchoolEventsExchange, //"school.events",
            MessagingConstants.SchoolSuspendedKey, //"school-suspended",
            new SchoolSuspended(school.Id, school.AdminUserId, DateTime.UtcNow),
            ct
        );
        return Result.Success();
    }
}
