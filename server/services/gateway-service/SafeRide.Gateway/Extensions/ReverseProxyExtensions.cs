using Microsoft.AspNetCore.HttpOverrides;
using SafeRide.Gateway.Transforms;

namespace SafeRide.Gateway.Extensions;

public static class ReverseProxyExtensions
{
    public static IServiceCollection AddGatewayProxy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms<IdentityHeaderTransformProvider>();

        return services;
    }

    public static IServiceCollection AddProxyHeaders(this IServiceCollection services)
    {
        // YARP adds X-Forwarded-* on the way out by default; this reads them on
        // the way in, so the gateway sees real client IPs if it is ever itself
        // behind another proxy.
        services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        );

        return services;
    }
}
