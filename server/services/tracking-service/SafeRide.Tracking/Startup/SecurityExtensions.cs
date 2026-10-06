using Microsoft.AspNetCore.Authentication;

namespace SafeRide.Tracking.Startup;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddAuthentication(GatewayAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, GatewayAuthenticationHandler>(
                GatewayAuthenticationHandler.SchemeName,
                _ => { }
            );

        services
            .AddAuthorizationBuilder()
            .AddPolicy("SchoolAdmin", p => p.RequireRole("SchoolAdmin"))
            .AddPolicy("Driver", p => p.RequireRole("Driver"))
            .AddPolicy("Parent", p => p.RequireRole("Parent"));

        return services;
    }
}
