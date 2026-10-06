namespace SafeRide.Tracking.Infrastructure;

/// <summary>
/// Carries the caller's identity onto service-to-service calls.
///
/// Trip start reads the route from Route and the roster from Student, and both
/// are tenant-scoped — so the call has to be made as the driver who triggered
/// it, not as "Tracking". A service token would lose the school scoping and
/// make every service trust Tracking to ask only for the right data.
///
/// Only the identity headers travel. No token is forwarded, because no service
/// validates one — the gateway strips it on the way in, and nothing behind the
/// gateway should be able to replay a caller's token elsewhere.
/// </summary>
public sealed class ForwardIdentityHandler(IHttpContextAccessor accessor) : DelegatingHandler
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
