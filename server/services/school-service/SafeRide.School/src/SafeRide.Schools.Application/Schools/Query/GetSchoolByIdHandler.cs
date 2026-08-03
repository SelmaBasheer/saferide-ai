using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Query;

public sealed class GetSchoolByIdHandler(ISchoolRepository schools)
{
    public async Task<Result<School>> GetAsync(Guid id, CancellationToken ct)
    {
        var school = await schools.GetWithDocumentsAsync(id, ct);
        return school is null
            ? Result.Failure<School>(SchoolErrors.SchoolNotFound)
            : Result.Success(school);
    }
}
