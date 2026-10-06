using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SafeRide.Tracking.Startup;

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

        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };

        // EmailUserIdProvider resolves Clients.User(...) from this claim, so a
        // parent stops receiving live updates if it goes missing — silently,
        // because SignalR simply delivers to nobody.
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
