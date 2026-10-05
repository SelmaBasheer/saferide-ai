namespace SafeRide.Gateway.Middleware;

/// <summary>
/// Keeps 401 and 403 in the same envelope the services return, so the
/// frontend's error reader works whether a request was rejected here or
/// downstream.
/// </summary>
public sealed class ErrorEnvelopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        var (code, message) = context.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => ("Auth.Unauthorized", "You are not signed in."),
            StatusCodes.Status403Forbidden => (
                "Auth.Forbidden",
                "You do not have access to this resource."
            ),
            _ => (null, null),
        };

        if (code is null)
        {
            return;
        }

        context.Response.ContentType = "application/json";

        // YARP copies the upstream response headers, including Content-Length.
        // A service that returns an empty 403 declares zero bytes, and writing
        // a body into that response throws — which the client sees as an empty
        // 500 instead of the 403 that actually happened.
        context.Response.Headers.ContentLength = null;

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

public static class ErrorEnvelopeExtensions
{
    public static IApplicationBuilder UseErrorEnvelope(this IApplicationBuilder app) =>
        app.UseMiddleware<ErrorEnvelopeMiddleware>();
}
