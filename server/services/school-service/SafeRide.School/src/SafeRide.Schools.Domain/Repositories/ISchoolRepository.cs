using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Domain.Repositories;

public interface ISchoolRepository : IGenericRepository<School>
{
    // "My school" for the logged-in admin — documents included
    Task<School?> GetByAdminUserIdAsync(Guid adminUserId, CancellationToken ct = default);

    // A school by id with documents loaded (SuperAdmin detail view, submit, etc.)
    Task<School?> GetWithDocumentsAsync(Guid id, CancellationToken ct = default);

    //Filter schools by status (for SuperAdmin list view)
    Task<IReadOnlyList<School>> ListByStatusAsync(
        SchoolStatus? status,
        CancellationToken ct = default
    );
}
