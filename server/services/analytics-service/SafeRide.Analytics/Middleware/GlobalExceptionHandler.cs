using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using SafeRide.Analytics.Common;

namespace SafeRide.Analytics.Middleware;

// Same envelope and the same shape as the Schools handler, with only the
// exception types this service can actually raise.
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
            AppException ex => (ex.StatusCode, ex.Code, ex.Message),
            // The read store being unreachable is a dependency failure, not a
            // bug in the request — 503 tells the caller to retry.
            SqlException => (
                StatusCodes.Status503ServiceUnavailable,
                "Infrastructure.Error",
                "A dependency is unavailable."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server.Error",
                "An unexpected error occurred."
            ),
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled: {Code}", code);
        else
            logger.LogWarning(exception, "Handled: {Code}", code);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(code, message),
            cancellationToken
        );
        return true;
    }
}
