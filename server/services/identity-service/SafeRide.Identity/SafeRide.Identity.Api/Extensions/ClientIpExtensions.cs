using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace SafeRide.Identity.Api.Extensions;

public static class ClientIpExtensions
{
    /// <summary>
    /// Behind the gateway every request arrives from the gateway's own address, so
    /// RemoteIpAddress would be identical for every user and per-IP rate limiting
    /// would put the whole school in a single bucket. This reads the original
    /// client address from X-Forwarded-For instead.
    ///
    /// KnownProxies is the security half: the header is client-settable, so it is
    /// trusted only when the connection came from a hop we control. Without it an
    /// attacker could forge a fresh IP on every request and evade rate limiting
    /// entirely — worse than never reading the header.
    /// </summary>
    public static IServiceCollection AddRealClientIp(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            options.KnownProxies.Clear();

            var trusted =
                configuration.GetSection("TrustedProxies").Get<string[]>() ?? ["127.0.0.1", "::1"];

            foreach (var proxy in trusted)
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }
        });

        return services;
    }
}
