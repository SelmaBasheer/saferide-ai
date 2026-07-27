using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeRide.School.Domain.Common;

namespace SafeRide.School.Api.Common;

public static class ResultExtensions
{
    // Maps a Result<TSource> to the standard envelope, projecting the value to a response DTO.
    public static IActionResult ToApiResponse<TSource, TResult>(
        this Result<TSource> result,
        Func<TSource, TResult> map,
        int successStatus = StatusCodes.Status200OK,
        string? successMessage = null
    )
    {
        if (result.IsSuccess)
            return new ObjectResult(ApiResponse<TResult>.Ok(map(result.Value), successMessage))
            {
                StatusCode = successStatus,
            };

        return new ObjectResult(ApiResponse<TResult>.Fail(result.Error.Code, result.Error.Message))
        {
            StatusCode = StatusCodes.Status400BadRequest,
        };
    }
}
