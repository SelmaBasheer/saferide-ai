using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Query;

public sealed class GetSchoolsHandler(ISchoolRepository schools)
{
    public async Task<IReadOnlyList<School>> GetAllAsync(
        SchoolStatus? status,
        CancellationToken ct
    ) => await schools.ListByStatusAsync(status, ct);
}
