using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Query;

public sealed class GetMySchoolHandler(ISchoolRepository schools)
{
    public async Task<Result<School>> GetAsync(Guid adminUserId, CancellationToken ct)
    {
        var school = await schools.GetByAdminUserIdAsync(adminUserId, ct);
        return school is null
            ? Result.Failure<School>(SchoolErrors.SchoolNotFound)
            : Result.Success(school);
    }
}
