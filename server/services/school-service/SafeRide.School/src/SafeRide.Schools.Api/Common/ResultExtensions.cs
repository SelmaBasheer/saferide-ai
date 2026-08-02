using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Common;

namespace SafeRide.Schools.Api.Common;

public static class ResultExtensions
{
    // Shape 1: success carries data.
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

    // Shape 2: success with no data.
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

    private static ObjectResult Failure<T>(Error error) =>
        new(ApiResponse<T>.Fail(error.Code, error.Message))
        {
            StatusCode = StatusForCode(error.Code),
        };

    // School's error codes -> HTTP status.
    private static int StatusForCode(string code) =>
        code switch
        {
            ErrorCodes.SchoolNotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };
}
