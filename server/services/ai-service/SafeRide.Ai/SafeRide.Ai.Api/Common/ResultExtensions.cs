using Microsoft.AspNetCore.Mvc;
using SafeRide.Ai.Application.Common;

namespace SafeRide.Ai.Api.Common;

public static class ResultExtensions
{
    // Shape 1: success carries data — map the handler's value to a response DTO.
    public static IActionResult ToApiResponse<TSource, TResult>(
        this Result<TSource> result,
        Func<TSource, TResult> map,
        int successStatus = StatusCodes.Status200OK,
        string? successMessage = null
    ) =>
        result.IsSuccess
            ? new ObjectResult(ApiResponse<TResult>.Ok(map(result.Value), successMessage))
            {
                StatusCode = successStatus,
            }
            : Failure<TResult>(result.Error);

    // Shape 2 & 4: success carries NO data — just an optional message.
    public static IActionResult ToApiResponse(
        this Result result,
        string? successMessage = null,
        int successStatus = StatusCodes.Status200OK
    ) =>
        result.IsSuccess
            ? new ObjectResult(ApiResponse<object?>.Ok(null, successMessage))
            {
                StatusCode = successStatus,
            }
            : Failure<object?>(result.Error);

    // One place that builds the failure envelope + picks the right status code.
    private static ObjectResult Failure<T>(Error error) =>
        new(ApiResponse<T>.Fail(error.Code, error.Message))
        {
            StatusCode = StatusForCode(error.Code),
        };

    // One place that maps error codes to HTTP status codes.
    private static int StatusForCode(string code) =>
        code switch
        {
            ErrorCodes.AnomalyNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.AlreadyResolved => StatusCodes.Status409Conflict,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };
}
