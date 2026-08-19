namespace SafeRide.Tracking.Common;

public class AppException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;

    public static AppException NotFound(string message) => new("Resource.NotFound", message, 404);

    public static AppException Conflict(string message) => new("Resource.Conflict", message, 409);

    public static AppException Forbidden(string message) => new("Auth.Forbidden", message, 403);

    public static AppException Validation(string message) => new("Validation.Error", message, 400);
}
