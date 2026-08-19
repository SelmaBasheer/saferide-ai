using System.Security.Claims;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Common;

public static class TripAccess
{
    public static bool CanView(Trip trip, ClaimsPrincipal user)
    {
        if (trip.SchoolId != user.SchoolId())
        {
            return false;
        }

        if (user.IsInRole("SchoolAdmin"))
        {
            return true;
        }

        if (user.IsInRole("Driver"))
        {
            return trip.DriverId == user.UserId();
        }

        if (user.IsInRole("Parent"))
        {
            return trip.Roster.Any(r =>
                string.Equals(r.ParentEmail, user.Email(), StringComparison.OrdinalIgnoreCase)
            );
        }

        return false;
    }

    public static bool CanEnd(Trip trip, ClaimsPrincipal user) =>
        trip.SchoolId == user.SchoolId()
        && (user.IsInRole("SchoolAdmin") || trip.DriverId == user.UserId());
}
