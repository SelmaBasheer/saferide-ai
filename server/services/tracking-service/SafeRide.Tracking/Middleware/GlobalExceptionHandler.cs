using Microsoft.AspNetCore.Diagnostics;
using SafeRide.Tracking.Common;

namespace SafeRide.Tracking.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (status, code, message) = exception switch
        {
            AppException app => (app.StatusCode, app.Code, app.Message),
            _ => (500, "Server.Error", "An unexpected error occurred."),
        };

        if (status == 500)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning("Handled: {Code}", code);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(code, message),
            cancellationToken
        );
        return true;
    }
}
