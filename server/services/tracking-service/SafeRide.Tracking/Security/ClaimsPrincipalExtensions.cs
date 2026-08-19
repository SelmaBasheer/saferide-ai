using System.Security.Claims;
using SafeRide.Tracking.Common;

namespace SafeRide.Tracking.Security;

//tracking's tenancy boundary
public static class ClaimsPrincipalExtensions
{
    public static Guid SchoolId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("schoolId");
        if (!Guid.TryParse(claim, out var schoolId))
        {
            throw AppException.Forbidden("No school context on this account.");
        }
        return schoolId;
    }

    public static string Email(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)
        ?? throw AppException.Forbidden("No email on this account.");

    public static Guid UserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
        {
            throw AppException.Forbidden("No user identity on this token.");
        }
        return userId;
    }
}
