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

    public async Task<(IReadOnlyList<School>, int)> SearchAsync(
        SchoolStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = Set.AsQueryable();
        if (status is not null)
            query = query.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.Name.Contains(search) || s.City.Contains(search) || s.AdminEmail.Contains(search)
            );

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}
