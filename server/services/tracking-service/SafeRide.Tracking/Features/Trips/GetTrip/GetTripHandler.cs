using System.Security.Claims;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Trips.Contracts;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Features.Trips.GetTrip;

public sealed class GetTripHandler(IMongoCollection<Trip> trips)
{
    public async Task<TripResponse> HandleAsync(
        Guid tripId,
        ClaimsPrincipal user,
        CancellationToken ct
    )
    {
        var trip =
            await trips.Find(t => t.Id == tripId).FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Trip not found.");

        if (!TripAccess.CanView(trip, user))
        {
            throw AppException.NotFound("Trip not found.");
        }

        // a parent may watch the trip, but only sees their own children
        if (user.IsInRole("Parent"))
        {
            var email = user.Email();

            var mine = trip
                .Roster.Where(r =>
                    string.Equals(r.ParentEmail, email, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            return trip.ToResponse(mine);
        }

        return trip.ToResponse();
    }
}
