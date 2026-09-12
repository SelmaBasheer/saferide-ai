using Microsoft.AspNetCore.Diagnostics;
using SafeRide.Ai.Api.Common;

namespace SafeRide.Ai.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        // Expected failures travel as Result and never reach here. Anything that does is a bug or an outage.
        logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail("Server.Error", "An unexpected error occurred."),
            cancellationToken
        );

        return true;
    }
}
