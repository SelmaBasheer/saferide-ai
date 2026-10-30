using AutoMapper;
using AutoMapper.QueryableExtensions;
using DriverService.Common;
using DriverService.Domain;
using DriverService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Features.GetDrivers;

public static class GetDriversEndpoint
{
    // A licence is "expiring" inside this window. Matches the warning the
    // school admin sees, so the tab and the banner can never disagree.
    private const int ExpiringSoonDays = 30;

    public static void MapGetDrivers(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/drivers", Handle).RequireAuthorization("SchoolAdmin");

    private static async Task<IResult> Handle(
        ICurrentUser currentUser,
        DriverDbContext db,
        IMapper mapper,
        string? search,
        string? status,
        string? licence,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default
    )
    {
        if (currentUser.SchoolId is not Guid schoolId)
            throw new ForbiddenException("No school context on this account.");

        page = Math.Clamp(page, 1, 1_000_000);
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

        if (
            !string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<DriverStatus>(status, ignoreCase: true, out var parsedStatus)
        )
        {
            query = query.Where(d => d.Status == parsedStatus);
        }

        // Licence state is filtered in the database rather than after paging.
        // Filtering a page's worth of rows in the browser would give a count
        // that describes the page, not the school — and a tab that reports the
        // wrong number is worse than no tab.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soonCutoff = today.AddDays(ExpiringSoonDays);

        query = licence?.ToLowerInvariant() switch
        {
            "expired" => query.Where(d => d.LicenseExpiryDate < today),
            "expiring" => query.Where(d =>
                d.LicenseExpiryDate >= today && d.LicenseExpiryDate <= soonCutoff
            ),
            "valid" => query.Where(d => d.LicenseExpiryDate > soonCutoff),
            _ => query,
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<DriverListItem>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Results.Ok(
            ApiResponse<PagedResult<DriverListItem>>.Ok(
                new PagedResult<DriverListItem>(items, total, page, pageSize)
            )
        );
    }
}
