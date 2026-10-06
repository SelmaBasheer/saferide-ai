using System.Security.Claims;
using SafeRide.Gateway.Common;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace SafeRide.Gateway.Transforms;

/// <summary>
/// Turns the validated token into identity headers, so services behind the
/// gateway read who the caller is without each one validating a JWT.
///
/// An ITransformProvider rather than an inline lambda: it applies to every
/// route without being listed on any of them, which means a route added later
/// cannot accidentally be left out.
/// </summary>
public sealed class IdentityHeaderTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(transform =>
        {
            var request = transform.ProxyRequest;

            // Removed first, always — even on anonymous routes, and even when
            // nothing replaces them. A client that can set its own identity
            // header has defeated the whole scheme, and "the route was
            // anonymous" is not a reason to let one through.
            foreach (var header in IdentityHeaders.All)
            {
                request.Headers.Remove(header);
            }

            // No service validates tokens any more, so none should see one.
            // This limits the blast radius of a compromised service: it cannot
            // replay the caller's token against anything else.
            request.Headers.Remove("Authorization");

            // SignalR sends its token in the query string, which would otherwise
            // end up in the proxied URL and in Tracking's request log. A token
            // in a log file is a token someone can use.
            transform.Query.Collection.Remove("access_token");

            var user = transform.HttpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return ValueTask.CompletedTask;
            }

            // sub carries the user id. .NET's inbound claim mapping renames it
            // to NameIdentifier; the fallback covers the case where someone
            // turns that mapping off.
            Add(
                IdentityHeaders.UserId,
                user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
            );

            Add(IdentityHeaders.SchoolId, user.FindFirstValue("schoolId"));
            Add(IdentityHeaders.Email, user.FindFirstValue(ClaimTypes.Email));

            // Comma-separated rather than repeated headers. A user has one role
            // today, but a list costs nothing now and avoids a breaking change
            // later if that stops being true.
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            if (roles.Length > 0)
            {
                Add(IdentityHeaders.Roles, string.Join(',', roles));
            }

            return ValueTask.CompletedTask;

            void Add(string name, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    request.Headers.TryAddWithoutValidation(name, value);
                }
            }
        });
    }
}
