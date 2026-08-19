using System.Security.Claims;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Trips.Contracts;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Features.Trips.ListTrips;

public sealed class ListTripsHandler(IMongoCollection<Trip> trips)
{
    private const int MaxPageSize = 50;

    public async Task<PagedResult<TripSummaryResponse>> HandleAsync(
        string? status,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken ct
    )
    {
        var safePage = Math.Max(page, 1);
        var safeSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var schoolId = user.SchoolId();
        var builder = Builders<Trip>.Filter;
        var filter = builder.Eq(t => t.SchoolId, schoolId);

        if (
            !string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<TripStatus>(status, true, out var parsed)
        )
        {
            filter &= builder.Eq(t => t.Status, parsed);
        }

        if (user.IsInRole("Driver"))
        {
            filter &= builder.Eq(t => t.DriverId, user.UserId());
        }
        else if (user.IsInRole("Parent"))
        {
            var parentEmail = user.Email().ToLowerInvariant();
            filter &= builder.ElemMatch(t => t.Roster, r => r.ParentEmail == parentEmail);
        }

        var total = await trips.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await trips
            .Find(filter)
            .SortByDescending(t => t.StartedAt)
            .Skip((safePage - 1) * safeSize)
            .Limit(safeSize)
            .ToListAsync(ct);

        return new PagedResult<TripSummaryResponse>(
            [.. items.Select(t => t.ToSummary())],
            total,
            safePage,
            safeSize
        );
    }
}
