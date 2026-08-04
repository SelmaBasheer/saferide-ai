using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Infrastructure.Persistence.Repositories;

public class SchoolRepository(SchoolDbContext context)
    : GenericRepository<School>(context),
        ISchoolRepository
{
    public async Task<School?> GetByAdminUserIdAsync(
        Guid adminUserId,
        CancellationToken ct = default
    ) =>
        await Set.Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.AdminUserId == adminUserId, ct);

    public async Task<School?> GetWithDocumentsAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(s => s.Documents).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<School>> ListByStatusAsync(
        SchoolStatus? status,
        CancellationToken ct = default
    ) =>
        await Set.Where(s => status == null || s.Status == status)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);
}
