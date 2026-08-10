using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed class UpdateSchoolProfileHandler(ISchoolRepository schools, IUnitOfWork unitOfWork)
{
    public async Task<Result> UpdateAsync(
        Guid adminUserId,
        UpdateSchoolProfileCommand cmd,
        CancellationToken ct
    )
    {
        var school = await schools.GetByAdminUserIdAsync(adminUserId, ct);
        if (school is null)
            return Result.Failure(SchoolErrors.SchoolNotFound);

        if (school.Status is not (SchoolStatus.Draft or SchoolStatus.Rejected))
            return Result.Failure(SchoolErrors.NotEditable);

        school.UpdateProfile(
            cmd.Name,
            cmd.Address,
            cmd.City,
            cmd.District,
            cmd.State,
            cmd.Pincode,
            cmd.LegalName,
            cmd.Board,
            cmd.RegistrationNumber,
            cmd.AuthorizedPersonName,
            cmd.AuthorizedPersonDesignation,
            cmd.OfficialPhone,
            cmd.OfficialEmail,
            cmd.BusCount,
            cmd.StudentCount
        );

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
