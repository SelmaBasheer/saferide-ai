using Microsoft.AspNetCore.Diagnostics;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Application.Common.Exceptions;
using SafeRide.Schools.Domain.Exceptions;
using SafeRide.Schools.Infrastructure.Exceptions;

namespace SafeRide.Schools.Api.Middleware;

// Global exception handling (.NET IExceptionHandler). Maps each exception type to a status
// code and returns the standard ApiResponse envelope.
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
            DomainException ex => (StatusCodes.Status400BadRequest, ex.Code, ex.Message),
            InfrastructureException => (
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
