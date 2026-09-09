using System.Text;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Layered over appsettings, so vault values win. When no vault is configured the
// app falls back to environment variables, which keeps it runnable by someone
// without an Azure subscription.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

var jwt = builder.Configuration.GetSection("JwtSettings");

var jwtSecret =
    jwt["Secret"]
    ?? throw new InvalidOperationException(
        "JwtSettings:Secret is not configured. Check the Key Vault URI, or set JwtSettings__Secret."
    );

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Must match Identity's AddJwtAuthentication exactly. Any drift and the
        // gateway rejects tokens the services would happily accept.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // A browser cannot set an Authorization header on a WebSocket handshake,
        // so SignalR puts the token in the query string. Without this, every hub
        // connection is rejected here even with a perfectly valid token.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];

                if (
                    !string.IsNullOrEmpty(token) && context.Request.Path.StartsWithSegments("/hubs")
                )
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// YARP adds X-Forwarded-* on the way out by default; this reads them on the way in,
// so the gateway itself sees real client IPs if it is ever behind another proxy.
app.UseForwardedHeaders();

// Keep 401 and 403 in the same envelope the services return, so the frontend's
// error reader works whether a request was rejected here or downstream.
app.Use(
    async (context, next) =>
    {
        await next();

        if (context.Response.HasStarted)
        {
            return;
        }

        if (
            context.Response.StatusCode
            is StatusCodes.Status401Unauthorized
                or StatusCodes.Status403Forbidden
        )
        {
            var (code, message) =
                context.Response.StatusCode == StatusCodes.Status401Unauthorized
                    ? ("Auth.Unauthorized", "You are not signed in.")
                    : ("Auth.Forbidden", "You do not have access to this resource.");

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(
                new
                {
                    success = false,
                    data = (object?)null,
                    message = (string?)null,
                    error = new { code, message },
                }
            );
        }
    }
);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapReverseProxy();

app.Run();
