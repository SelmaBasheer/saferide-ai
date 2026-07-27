namespace SafeRide.Identity.Application.Common.Exceptions;

public class AppException(string code, string message, int statusCode = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class NotFoundException(string message, string code = "Resource.NotFound")
    : AppException(code, message, 404);

public sealed class ConflictException(string message, string code = "Resource.Conflict")
    : AppException(code, message, 409);

public sealed class ForbiddenException(string message, string code = "Auth.Forbidden")
    : AppException(code, message, 403);
