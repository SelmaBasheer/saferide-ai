using DriverService.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DriverService.Middleware;

// Global exception handling (.NET IExceptionHandler). Maps each exception type to a status
// code and returns the standard ApiResponse envelope — same contract as School/Identity.
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

            // DB-level: unique index violation (e.g. duplicate email per school)
            DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } => (
                StatusCodes.Status409Conflict,
                "Db.Duplicate",
                ResponseMessages.DriverEmailExists
            ),

            // DB-level: optimistic concurrency
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Db.Concurrency",
                "The record was changed by another request. Please try again."
            ),

            // DB-level: other constraint violations (FK, not-null, check)
            DbUpdateException { InnerException: PostgresException } => (
                StatusCodes.Status400BadRequest,
                "Db.ConstraintViolation",
                "The request could not be completed due to a data constraint."
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

        return true; // handled — stop propagation
    }
}
