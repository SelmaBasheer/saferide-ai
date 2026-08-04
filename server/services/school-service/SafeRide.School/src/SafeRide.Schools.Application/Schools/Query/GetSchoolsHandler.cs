using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Query;

public sealed class GetSchoolsHandler(ISchoolRepository schools)
{
    public async Task<(IReadOnlyList<School> Items, int TotalCount)> GetAllAsync(
        SchoolStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await schools.SearchAsync(status, search, page, pageSize, ct);
    }
}
