using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Identity.Application.Common;

namespace SafeRide.Identity.Api.Common;

public static class ResultExtensions
{
    // Maps the handler's Result<TSource> into the standard envelope,
    // mapping the value to a response DTO along the way.
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
            StatusCode = StatusForCode(result.Error.Code),
        };
    }

    // Central place that maps error codes to HTTP status codes.
    private static int StatusForCode(string code) =>
        code switch
        {
            "Auth.InvalidCredentials" or "Auth.InvalidRefreshToken" or "Auth.AccountNotActive" =>
                StatusCodes.Status401Unauthorized,
            "Auth.EmailTaken" => StatusCodes.Status409Conflict,
            "Validation.Failed" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };
}
