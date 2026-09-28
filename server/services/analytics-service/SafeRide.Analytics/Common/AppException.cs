namespace SafeRide.Analytics.Common;

/// Analytics has no domain model to throw from — it reads rows and renders
/// reports. One exception type covers the only failures worth distinguishing:
/// a request that asks for something invalid.
public sealed class AppException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;

    public static AppException BadRequest(string code, string message) =>
        new(StatusCodes.Status400BadRequest, code, message);

    public static AppException NotFound(string code, string message) =>
        new(StatusCodes.Status404NotFound, code, message);
}
