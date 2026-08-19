using System.Security.Claims;
using MongoDB.Driver;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Trips.Contracts;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Features.Trips.EndTrip.GetActiveTrips;

public sealed class GetActiveTripsHandler(IMongoCollection<Trip> trips)
{
    public async Task<List<TripSummaryResponse>> HandleAsync(
        ClaimsPrincipal user,
        CancellationToken ct
    )
    {
        var schoolId = user.SchoolId();

        var active = await trips
            .Find(t => t.SchoolId == schoolId && t.Status == TripStatus.Active)
            .ToListAsync(ct);

        return [.. active.Select(t => t.ToSummary())];
    }
}
