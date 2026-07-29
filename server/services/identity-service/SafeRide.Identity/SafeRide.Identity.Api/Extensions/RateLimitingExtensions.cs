using System.Threading.RateLimiting;
using SafeRide.Identity.Api.Common;

namespace SafeRide.Identity.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // per-IP: 3 requests / minute for OTP request endpoints
            options.AddPolicy("otp", ByIp(permitLimit: 3));

            // per-IP: 5 attempts / minute for the OTP guess endpoint
            options.AddPolicy("otp-verify", ByIp(permitLimit: 5));

            // uniform 429 — same for everyone, reveals nothing
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object?>.Fail(
                        "Rate.Limit",
                        "Too many requests. Please wait a minute and try again."
                    ),
                    token
                );
            };
        });

        return services;
    }

    private static Func<HttpContext, RateLimitPartition<string>> ByIp(int permitLimit) =>
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }
            );
}
