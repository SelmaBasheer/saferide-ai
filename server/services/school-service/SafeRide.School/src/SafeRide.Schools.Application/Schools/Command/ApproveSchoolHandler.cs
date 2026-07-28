using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Common;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed class ApproveSchoolHandler(
    IGenericRepository<School> schools,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher
)
{
    public async Task<Result> ApproveAsync(Guid id, CancellationToken ct)
    {
        var school = await schools.GetByIdAsync(id, ct);
        if (school is null)
            return Result.Failure(new Error("School.NotFound", "School not found."));

        school.Approve();
        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            "school.events",
            "school-approved",
            new SchoolApproved(school.Id, school.AdminUserId, DateTime.UtcNow),
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
            "school.events",
            "school-suspended",
            new SchoolSuspended(school.Id, school.AdminUserId, DateTime.UtcNow),
            ct
        );
        return Result.Success();
    }
}
