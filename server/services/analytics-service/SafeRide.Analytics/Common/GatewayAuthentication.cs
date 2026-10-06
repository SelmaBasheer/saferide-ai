using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SafeRide.Analytics.Common;

public sealed class GatewayAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Gateway";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-SafeRide-UserId"].ToString();

        // No header means the request did not come through the gateway, or came
        // through it unauthenticated. Either way there is no identity to build.
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };

        Add(ClaimTypes.Email, Request.Headers["X-SafeRide-Email"]);
        Add("schoolId", Request.Headers["X-SafeRide-SchoolId"]);

        foreach (
            var role in Request
                .Headers["X-SafeRide-Roles"]
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        )
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Passing the scheme name as the authentication type is what makes
        // IsAuthenticated true — without it every policy would reject this.
        var identity = new ClaimsIdentity(claims, SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)
            )
        );

        void Add(string type, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                claims.Add(new Claim(type, value));
        }
    }
}

public static class GatewayAuthenticationExtensions
{
    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services) =>
        services
            .AddAuthentication(GatewayAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, GatewayAuthenticationHandler>(
                GatewayAuthenticationHandler.SchemeName,
                _ => { }
            )
            .Services;
}
