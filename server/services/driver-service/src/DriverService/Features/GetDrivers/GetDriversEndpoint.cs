using DriverService.Common;
using DriverService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Features.GetDrivers;

public static class GetDriversEndpoint
{
    public static void MapGetDrivers(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/drivers", Handle).RequireAuthorization();

    private static async Task<IResult> Handle(
        ICurrentUser currentUser,
        DriverDbContext db,
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default
    )
    {
        if (currentUser.SchoolId is not Guid schoolId)
            return Results.Forbid();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Drivers.AsNoTracking().Where(d => d.SchoolId == schoolId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.FirstName, pattern)
                || EF.Functions.ILike(d.LastName, pattern)
                || EF.Functions.ILike(d.Email, pattern)
                || EF.Functions.ILike(d.LicenseNumber, pattern)
            );
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => DriverListItem.From(d))
            .ToListAsync(ct);

        return Results.Ok(new PagedResult<DriverListItem>(items, total, page, pageSize));
    }
}
