using System.Security.Claims;

namespace SafeRide.Ai.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// Always from the token, never from a request body.
    public static Guid SchoolId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("schoolId"), out var id)
            ? id
            : throw new InvalidOperationException("No school context on this account.");

    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("No user id on this token.");

    /// Both forms, because the short name is what a hand-built token usually
    /// carries and the long URI is what ASP.NET maps it to.
    public static string Email(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? string.Empty;
}
