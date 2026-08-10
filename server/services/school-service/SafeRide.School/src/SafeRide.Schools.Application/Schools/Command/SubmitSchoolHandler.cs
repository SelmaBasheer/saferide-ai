using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed class SubmitSchoolHandler(
    ISchoolRepository schools,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher
)
{
    public async Task<Result> SubmitAsync(Guid adminUserId, CancellationToken ct)
    {
        var school = await schools.GetByAdminUserIdAsync(adminUserId, ct);
        if (school is null)
            return Result.Failure(SchoolErrors.SchoolNotFound);

        if (school.Status is not (SchoolStatus.Draft or SchoolStatus.Rejected))
            return Result.Failure(SchoolErrors.NotEditable);

        var missing = school.GetMissingRequirements();
        if (missing.Count > 0)
            return Result.Failure(
                new Error(
                    ErrorCodes.IncompleteSubmission,
                    $"Cannot submit yet. Missing: {string.Join(", ", missing)}."
                )
            );

        school.Submit();
        await unitOfWork.SaveChangesAsync(ct);

        await publisher.PublishAsync(
            MessagingConstants.SchoolEventsExchange, //"school.events",
            MessagingConstants.SchoolSubmittedForApprovalKey, //"school-submitted-for-approval",
            new SchoolSubmittedForApproval(
                school.Id,
                school.Name,
                school.AdminEmail,
                school.SubmittedAtUtc!.Value
            ),
            ct
        );

        return Result.Success();
    }
}
