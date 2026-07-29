using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed class GetSchoolsHandler(IGenericRepository<School> schools)
{
    public Task<IReadOnlyList<School>> GetAllAsync(CancellationToken ct) => schools.ListAsync(ct);
}
