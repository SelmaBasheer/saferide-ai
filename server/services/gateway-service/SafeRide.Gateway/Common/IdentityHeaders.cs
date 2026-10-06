namespace SafeRide.Gateway.Common;

/// <summary>
/// The contract between the gateway and every service behind it.
///
/// The prefix is deliberate: these must never collide with a header a client,
/// a proxy or a load balancer might legitimately set. The gateway removes all
/// of them from the inbound request before adding its own — without that, a
/// client simply sets X-SafeRide-SchoolId and reads another school's data.
/// </summary>
public static class IdentityHeaders
{
    public const string UserId = "X-SafeRide-UserId";
    public const string SchoolId = "X-SafeRide-SchoolId";
    public const string Email = "X-SafeRide-Email";
    public const string Roles = "X-SafeRide-Roles";

    public static readonly string[] All = [UserId, SchoolId, Email, Roles];
}
