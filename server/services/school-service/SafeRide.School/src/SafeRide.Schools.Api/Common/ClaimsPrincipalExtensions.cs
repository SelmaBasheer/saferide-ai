using System.Security.Claims;

namespace SafeRide.Schools.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(
            user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? throw new InvalidOperationException("Token has no user id claim.")
        );
}
