using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Identity.Api.Common;
using SafeRide.Identity.Api.Contracts;
using SafeRide.Identity.Application.Auth.Login;
using SafeRide.Identity.Application.Auth.Password;
using SafeRide.Identity.Application.Auth.Refresh;
using SafeRide.Identity.Application.Auth.Register;

namespace SafeRide.Identity.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    RegisterSchoolAdminHandler registerHandler,
    LoginHandler loginHandler,
    RefreshTokenHandler refreshHandler
) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register/school-admin")]
    public async Task<IActionResult> RegisterSchoolAdmin(
        [FromBody] RegisterSchoolAdminCommand command,
        CancellationToken ct
    )
    {
        var result = await registerHandler.HandleAsync(command, ct);
        return result.ToApiResponse(
            id => new RegisterResponse(id),
            StatusCodes.Status201Created,
            "School administrator registered successfully."
        );
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await loginHandler.HandleAsync(command, ct);
        return result.ToApiResponse(v => new AuthResponse(
            v.AccessToken,
            v.RefreshToken,
            v.AccessTokenExpiresAtUtc
        ));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken ct
    )
    {
        var result = await refreshHandler.HandleAsync(command, ct);
        return result.ToApiResponse(v => new AuthResponse(
            v.AccessToken,
            v.RefreshToken,
            v.AccessTokenExpiresAtUtc
        ));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        [FromServices] ForgotPasswordHandler handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(command, ct);
        // always the same response — no account enumeration
        return Ok(ApiResponse<object?>.Ok(null, "An OTP has been sent to the registered email."));
    }

    [AllowAnonymous]
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendOtpCommand command,
        [FromServices] ResendOtpHandler handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object?>.Ok(null, "A new OTP has been sent to the registered email."))
            : BadRequest(ApiResponse<object?>.Fail(result.Error.Code, result.Error.Message)); // cooldown
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        [FromServices] ResetPasswordHandler handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object?>.Ok(null, "Password reset successfully."))
            : BadRequest(ApiResponse<object?>.Fail(result.Error.Code, result.Error.Message));
    }
}
