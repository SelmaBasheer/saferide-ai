using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SafeRide.Gateway.Extensions;

public static class AuthenticationExtensions
{
    public const string AuthenticatedPolicy = "authenticated";

    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwt = configuration.GetSection("JwtSettings");

        var secret =
            jwt["Secret"]
            ?? throw new InvalidOperationException(
                "JwtSettings:Secret is not configured. Check the Key Vault URI, or set JwtSettings__Secret."
            );

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // This is now the only place a token is validated. Everything
                // behind the gateway trusts the headers it adds, so a mistake
                // here is a mistake everywhere.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // A browser cannot set an Authorization header on a
                        // WebSocket handshake, so SignalR puts the token in the
                        // query string. Without this, every hub connection is
                        // rejected here even with a perfectly valid token.
                        var token = context.Request.Query["access_token"];

                        if (
                            !string.IsNullOrEmpty(token)
                            && context.Request.Path.StartsWithSegments("/hubs")
                        )
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization(options =>
            options.AddPolicy(AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser())
        );

        return services;
    }
}
