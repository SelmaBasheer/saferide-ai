namespace SafeRide.Tracking.Infrastructure;

/// <summary>
/// Carries the caller's identity onto service-to-service calls.
///
/// Trip start reads the route from Route and the roster from Student, and both
/// are tenant-scoped — so the call has to be made as the driver who triggered
/// it, not as "Tracking". A service token would lose the school scoping and
/// make every service trust Tracking to ask only for the right data.
///
/// Both forms are forwarded during the migration: Authorization for services
/// still validating tokens, identity headers for those that have moved to
/// reading them from the gateway. The Authorization line comes out once every
/// service is migrated.
/// </summary>
public sealed class ForwardAuthHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    private static readonly string[] IdentityHeaders =
    [
        "X-SafeRide-UserId",
        "X-SafeRide-SchoolId",
        "X-SafeRide-Email",
        "X-SafeRide-Roles",
    ];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var incoming = accessor.HttpContext?.Request;

        // No HttpContext means a background job, not a user request. Nothing to
        // forward, and nothing should be invented.
        if (incoming is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        if (
            incoming.Headers.TryGetValue("Authorization", out var authorization)
            && !request.Headers.Contains("Authorization")
        )
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());
        }

        foreach (var name in IdentityHeaders)
        {
            if (!incoming.Headers.TryGetValue(name, out var value))
            {
                continue;
            }

            // Removed first so a retried request cannot accumulate duplicates.
            request.Headers.Remove(name);
            request.Headers.TryAddWithoutValidation(name, value.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
